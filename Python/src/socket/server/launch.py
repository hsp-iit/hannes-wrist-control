import copy
import pickle
import glob
import socket
import sys
import os
import io
import struct
import argparse
import yaml

from PIL import Image
import numpy as np
from pytransform3d.rotations import active_matrix_from_intrinsic_euler_zyx
from pytransform3d.transformations import transform_from

# make sure that the file is run from the Python folder
# (i.e. python src/socket/server/launch.py)
assert os.path.basename(os.getcwd()) == 'Python'

sys.path.append(os.getcwd())
from src.socket.server import communication_utils as cu
from src.utils.unity import unity_to_renderer_camera_refframe

sys.path.append(os.path.join('libs', 'visual-servoing'))
from utils.renderer import Renderer
from robots.spatial2r import Spatial2R
from vservo.vservo import build_vservo


def main(
    socket_config,
    vservo_config,
    obj_model_path,
    dump_measures,
    n_iters,
):
    ## Create renderer (note that height and width must match the ones of the 
    ## image that we receive from Unity)
    img_height = vservo_config['IBVSBase']['img_height']
    img_width = vservo_config['IBVSBase']['img_width']

    # NOTE This is the Field of View of the Wrist Camera object in Unity. 
    #      In this Unity object, make sure that:
    #      - Projection == Perspective
    #      - FOV Axis == Vertical
    #      - Field of View == 42.66058
    UNITY_VERTICAL_FOV_DEGREES = 42.66058
    # intrinsic parameters of the camera
    # [fx  0   cx]
    # [0   fy  cy]
    # [0   0    1]
    # Compute intrinsics from vertical FOV and image size (assuming square pixels)
    fovy_rad = np.deg2rad(UNITY_VERTICAL_FOV_DEGREES)
    fy = (img_height / 2.0) / np.tan(fovy_rad / 2.0)
    fx = fy  # square pixels
    cx = img_width / 2.0
    cy = img_height / 2.0
    intr_matr = np.array([[fx, 0.0, cx],
                          [0.0, fy, cy],
                          [0.0, 0.0, 1.0]], dtype=np.float64)
    
    ren = Renderer(img_height, img_width, intr_matr)
    ren.add_object(0, obj_model_path)

    ## Initialize prosthesis kinematic chain
    robot_chain = Spatial2R(
        vservo_config["spatial2r"]["L1"], vservo_config["spatial2r"]["L2"]
    )
    R_e2c = active_matrix_from_intrinsic_euler_zyx(
        np.deg2rad(
            [
                vservo_config["spatial2r"]["eul_e2c_deg"]["z"],
                vservo_config["spatial2r"]["eul_e2c_deg"]["y"],
                vservo_config["spatial2r"]["eul_e2c_deg"]["x"],
            ]
        )
    )
    t_e2c = np.array(
        [
            vservo_config["spatial2r"]["t_e2c"]["x"],
            vservo_config["spatial2r"]["t_e2c"]["y"],
            vservo_config["spatial2r"]["t_e2c"]["z"],
        ]
    )
    M_e2c = transform_from(R_e2c, t_e2c)

    ## Initialize visual servoing
    full_vservo_args = copy.deepcopy(vservo_config['IBVSBase'])
    full_vservo_args.update(vservo_config[vservo_config['vservo_name']])
    full_vservo_args['intr_matr'] = intr_matr
    visual_servo = build_vservo(vservo_config['vservo_name'], full_vservo_args)

    lmbda_psfe = np.array(
        [vservo_config["spatial2r"]["lmbda_ps"], vservo_config["spatial2r"]["lmbda_fe"]]
    )
    draw = vservo_config['draw']

    ## Create TCP socket
    conn, s_send = None, None
    # TCP connection
    s = socket.socket(socket.AF_INET, socket.SOCK_STREAM) 
    # https://stackoverflow.com/questions/4465959/python-errno-98-address-already-in-use
    s.setsockopt(socket.SOL_SOCKET, socket.SO_REUSEADDR, 1)
    s.bind(
        (socket_config["SOCKET"]["ip_server"], socket_config["SOCKET"]["port_server"])
    )
    s.listen()
    print('===== READY TO RECEIVE CONNECTION REQUEST FROM THE CLIENT =====')
    conn, _ = s.accept()
    try:
        receive_and_process_data(
            conn,
            dump_measures,
            n_iters,
            ren,
            visual_servo,
            robot_chain,
            M_e2c,
            lmbda_psfe,
            draw,
        )
    except Exception as e:
        conn.close()
        raise e


def receive_and_process_data(
    conn,
    dump_measures,
    n_iters,
    ren,
    visual_servo,
    robot_chain,
    M_e2c,
    lmbda_psfe,
    draw
):
    elements_received = []
    count_elements_received = 0

    bytesReadHeader = 0
    bytesReadPayloadLength = 0
    bytesReadPayload = 0

    payloadLength = 0

    rawHeader = bytearray(socket_config['SOCKET']['header_num_bytes'])
    rawPayloadLength = bytearray(socket_config['SOCKET']['payloadLength_num_bytes'])
    rawPayload = None

    if dump_measures:
        measures = dict()
        measures['wrist_rgb_image'] = []
        measures['external_rgb_image'] = []
        measures['ps_vel'] = []
        measures['fe_vel'] = []
        measures['error'] = []
    max_iters = n_iters if n_iters > 0 else -1
    count_iters = 0

    ## Receive and read data, perform computations (i.e. visual servoing) and
    ## send responde back to the client
    while True:
        data = conn.recv(socket_config['SOCKET']['block_size'])

        if not data:
            break

        bytes_read = len(data)

        for i in range(bytes_read):
            if bytesReadHeader < socket_config['SOCKET']['header_num_bytes']:
                rawHeader[bytesReadHeader] = data[i]
                bytesReadHeader += 1

            elif bytesReadPayloadLength < socket_config['SOCKET']['payloadLength_num_bytes']:
                rawPayloadLength[bytesReadPayloadLength] = data[i]
                bytesReadPayloadLength += 1

                if bytesReadPayloadLength == socket_config['SOCKET']['payloadLength_num_bytes']:
                    payloadLength = int.from_bytes(rawPayloadLength, byteorder="big")
                    rawPayload = bytearray(payloadLength)

            elif bytesReadPayload < payloadLength:
                rawPayload[bytesReadPayload] = data[i]
                bytesReadPayload += 1

                if bytesReadPayload == payloadLength:
                    # All the packet bytes have been received, start
                    # reading it.

                    header_received = rawHeader.decode('utf-8').strip()

                    expected_header = socket_config["COMMUNICATION"][
                        "server_elements_to_receive"
                    ][count_elements_received]
                    if header_received != expected_header:
                        raise Exception(
                            'Wrong header type received. '
                            'Expected: {}, received: {}'
                            .format(expected_header, header_received)
                        ) 

                    # Extract real payload type from the raw payload
                    if header_received == 'string':
                        payload = rawPayload.decode('utf-8')
                    elif header_received == 'image':
                        payload = Image.open(io.BytesIO(rawPayload))
                    elif header_received == 'vector3':
                        x = struct.unpack('>f', rawPayload[0:4])[0]
                        y = struct.unpack('>f', rawPayload[4:8])[0]
                        z = struct.unpack('>f', rawPayload[8:12])[0]
                        # vector3 is represented as a tuple
                        payload = (x, y, z)
                    elif header_received == 'float':
                        payload = struct.unpack('>f', rawPayload)[0]
                    else:
                        raise Exception('Unknown header type received: {}'.format(header_received))

                    # Update stuff about elements received
                    elements_received.append(payload)
                    count_elements_received += 1

                    # Reset stuff about packet reading
                    bytesReadHeader = 0
                    bytesReadPayloadLength = 0
                    bytesReadPayload = 0

                    # If all the elements (i.e. all the packets) are
                    # received, perform a visual servoing step
                    if count_elements_received == len(socket_config['COMMUNICATION']['server_elements_to_receive']):

                        ## read elements received

                        t_c2o = np.array(elements_received[0])
                        # meters (from unity) to millimeters since the ply
                        # models we are using here are in millimeters
                        t_c2o *= 1000       
                        R_c2o = np.array(elements_received[1])
                        M_c2o = unity_to_renderer_camera_refframe(
                            *R_c2o, *t_c2o
                        )

                        cur_ps_eul_deg = elements_received[2]
                        cur_fe_eul_deg = elements_received[3]
                        cur_theta_eul_deg = np.array(
                            [cur_ps_eul_deg, cur_fe_eul_deg]
                        )

                        wrist_rgb_image = elements_received[4]
                        wrist_rgb_image = np.array(wrist_rgb_image)
                        external_rgb_image = elements_received[5]
                        external_rgb_image = np.array(external_rgb_image)

                        ## render object

                        ren_rgb = ren.render_obj_on_img(0, M_c2o)
                        assert (
                            wrist_rgb_image.shape == ren_rgb.shape
                        ), f"image received from Unity must be of shape {ren_rgb.shape}, but it is {wrist_rgb_image.shape}"
                        mask_bool = ren_rgb.sum(axis=-1)
                        mask_bool = mask_bool != 0

                        ## run visual servoing step

                        if not np.sum(mask_bool):
                            theta_dot_rads = np.array([0, 0])
                        else:
                            theta_dot_rads, error = visual_servo.step(
                                mask_bool,
                                robot_chain,
                                M_e2c,
                                cur_theta_eul_deg,
                                lmbda_psfe,
                                wrist_rgb_image,
                                draw,
                            )
                            assert theta_dot_rads.shape == (2,)
                            # error.shape == (n_points, 2)
                            assert error.ndim == 2 and error.shape[1] == 2

                            error_norm = np.sum(np.linalg.norm(error, axis=1))

                        ## Send back velocities

                        cu.Sender.SendFloat(
                            theta_dot_rads[0], 
                            conn,
                            socket_config['SOCKET']['header_num_bytes'],
                            socket_config['SOCKET']['payloadLength_num_bytes'],
                        )
                        cu.Sender.SendFloat(
                            theta_dot_rads[1], 
                            conn,
                            socket_config['SOCKET']['header_num_bytes'],
                            socket_config['SOCKET']['payloadLength_num_bytes'],
                        )

                        # Reset stuff about elements received
                        count_elements_received = 0
                        elements_received = []

                        # Few other stuff..

                        if dump_measures:
                            measures['wrist_rgb_image'].append(wrist_rgb_image)
                            measures['external_rgb_image'].append(external_rgb_image)
                            measures['ps_vel'].append(theta_dot_rads[0])
                            measures['fe_vel'].append(theta_dot_rads[1])
                            measures['error'].append(error_norm)

                        count_iters += 1
                        if count_iters == max_iters:
                            break

            else:
                raise Exception('Something went wrong...')

    if dump_measures:
        os.makedirs('measures', exist_ok=True)
        files = glob.glob(os.path.join('measures', 'exp_'+'[0-9]'*6+'.pkl'))
        if len(files):
            files.sort()
            id = int(os.path.basename(files[-1]).split('exp_')[1].split('.pkl')[0])
            id += 1
        else:
            id = 1
        measures_path = os.path.join('measures', 'exp_'+str(id).zfill(6)+'.pkl')
        with open(measures_path, 'wb') as f:
            pickle.dump(measures, f)
        print(f"measures dumped at {measures_path}")


if __name__ == '__main__':
    parser = argparse.ArgumentParser()
    parser.add_argument(
        '--socket_config', type=str, default='src/socket/socket_config.yaml'
    )
    parser.add_argument(
        '--vservo_config', type=str, default='libs/visual-servoing/configs/conf.yaml'
    )
    parser.add_argument(
        '--obj_model_path', type=str, default="assets/obj_000005.ply"
    )
    parser.add_argument('--dump_measures', action='store_true')
    parser.add_argument('--n_iters', type=int, default=0)
    args = parser.parse_args()

    with open(args.socket_config, 'r') as f:
        socket_config = yaml.full_load(f)
    with open(args.vservo_config, 'r') as f:
        vservo_config = yaml.full_load(f)

    main(
        socket_config,
        vservo_config,
        args.obj_model_path,
        args.dump_measures,
        args.n_iters,
    )
