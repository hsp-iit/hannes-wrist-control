import argparse
import pickle
import multiprocessing

import matplotlib.pyplot as plt
import numpy as np
import cv2


def make_2_plots(n_iters, error_range, vel_range):
    fig, axs = plt.subplots(2, figsize=(6.4, 9.6))

    # Before entering the iterations loop, set here below stuff that
    # remains fixed at every iteration

    # ERROR STUFF
    axs[0].set_xlim(0, n_iters - 1)
    axs[0].set_ylim(0, error_range)
    axs[0].set_xlabel('iteration')
    axs[0].axhline(y=0, color='grey', alpha=0.4)
    axs[0].set_ylabel('feature error')
    axs[0].set_title('Error')

    # VELOCITIES STUFF
    axs[1].set_xlim(0, n_iters - 1)
    axs[1].set_ylim(-vel_range, vel_range)
    axs[1].axhline(y=0, color='grey', alpha=0.4)
    axs[1].set_xlabel('iteration')
    axs[1].set_ylabel('rot. velocity (rad/s)')
    axs[1].set_title('Velocities')

    return fig, axs


def process_step(
    wrist_rgb_image,
    external_rgb_image,
    n_iters,
    idx,
    error,
    ps_vel,
    fe_vel,
    ERROR_RANGE,
    VEL_RANGE,
    images
):
    w_e_images = cv2.vconcat([wrist_rgb_image, external_rgb_image])

    fig, axs = make_2_plots(n_iters, ERROR_RANGE, VEL_RANGE)

    axs[0].plot(np.arange(idx), error, color="red")

    axs[1].plot(np.arange(idx), ps_vel, label="WPS", color="blue")
    axs[1].plot(np.arange(idx), fe_vel, label="WFE", color="cyan")
    axs[1].legend(loc="upper right")

    plt.tight_layout()

    # get image (as numpy array) from current plot
    fig = plt.gcf()
    fig.canvas.draw()
    cur_plot = np.frombuffer(fig.canvas.tostring_rgb(), dtype=np.uint8)
    cur_plot = cur_plot.reshape(fig.canvas.get_width_height()[::-1] + (3,))
    plt.close()

    final_img = cv2.hconcat([w_e_images, cur_plot])[..., ::-1]
    images[idx] = final_img


def main(measures_exp_path):
    with open(measures_exp_path, "rb") as f:
        measures = pickle.load(f)

    ERROR_RANGE = 0.5
    VEL_RANGE = 0.005

    manager = multiprocessing.Manager()
    images = manager.dict()
    cpu_count = multiprocessing.cpu_count()
    pool = multiprocessing.Pool(processes=cpu_count)

    n_iters = len(measures["error"])
    for idx in range(1, n_iters + 1):
        wrist_rgb_image = measures['wrist_rgb_image'][idx - 1]
        external_rgb_image = measures['external_rgb_image'][idx - 1]
        ps_vel = measures['ps_vel'][0:idx]
        fe_vel = measures['fe_vel'][0:idx]
        error = measures['error'][0:idx]

        pool.apply_async(
            process_step,
            args=(
                wrist_rgb_image,
                external_rgb_image,
                n_iters,
                idx,
                error,
                ps_vel,
                fe_vel,
                ERROR_RANGE,
                VEL_RANGE,
                images,
            ),
        )
    pool.close()
    pool.join()

    idx = 1
    while idx < len(images):
        img = images[idx]
        cv2.imshow("plots", img)
        key = cv2.waitKey(0)
        if key == ord("q"):
            cv2.destroyAllWindows()
            exit()
        elif key == ord("p"):
            idx -= 1
            if idx < 1:
                idx = 1
        else:
            idx += 1


if __name__ == "__main__":
    parser = argparse.ArgumentParser()
    parser.add_argument('--measures_exp_path', type=str, required=True)
    args = parser.parse_args()

    main(args.measures_exp_path)
