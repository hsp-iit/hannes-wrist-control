import numpy as np
from pytransform3d.rotations import active_matrix_from_extrinsic_euler_zxy
from pytransform3d.transformations import transform_from


def unity_to_renderer_camera_refframe(
    r_x_unity,
    r_y_unity,
    r_z_unity,
    t_x_unity,
    t_y_unity,
    t_z_unity
):
    # correction 1: left to right-handed coordinate system
    R_c2o = active_matrix_from_extrinsic_euler_zxy(
        np.deg2rad([-r_z_unity, r_x_unity, -r_y_unity])
    )
    t_c2o = np.array([-t_x_unity, t_y_unity, t_z_unity])   
    M_c2o = transform_from(R_c2o, t_c2o)

    # correction 2: rotate camera 180 about z axis
    R_z180 = active_matrix_from_extrinsic_euler_zxy(np.deg2rad([180, 0, 0]))
    M_z180 = transform_from(R_z180, np.array([0, 0, 0]))
    M_c2o = np.matmul(M_z180, M_c2o)

    return M_c2o
