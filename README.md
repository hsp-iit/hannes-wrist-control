<p align="center">
  <h1 align="center">Continuous Wrist Control on the Hannes Prosthesis: <br>a Vision-based Shared Autonomy Framework</h1>
  <p align="center">
    <a href="https://federicovasile1.github.io">Federico Vasile</a>,
    <a href="https://www.iit.it/it/people-details/-/people/elisa-maiettini">Elisa Maiettini</a>,
    <a href="https://scholar.google.it/citations?user=8EKDQjcAAAAJ&hl=it">Giulia Pasquale</a>,
    <a href="https://www.iit.it/it/people-details/-/people/nicolo-boccardo">Nicolò Boccardo</a>,
    <a href="https://hsp.iit.it/people-details/-/people/lorenzo-natale">Lorenzo Natale</a>
  </p>
  <p align="center">
    <a href='https://arxiv.org/abs/2502.17265'>
      <img src='https://img.shields.io/badge/Paper-PDF-red?style=flat&logo=arXiv&logoColor=red' alt='Paper PDF'>
    </a>
    <a href='https://hsp-iit.github.io/hannes-wrist-control/' style='padding-left: 0.5rem;'>
      <img src='https://img.shields.io/badge/Project-Page-blue?style=flat&logo=Google%20chrome&logoColor=blue' alt='Project Page'>
    </a>
  </p>
   
  <p align="center">
    <b>TL;DR: Simulation environment for wrist control from an eye-in-hand camera</b>
    <br><br>
    <img src="teaser.gif">
  </p>
  <p align="center">
    <i>(check out also our <a href="https://github.com/hsp-iit/dinov2det">DINOv2Det</a> for instance segmentation and the <a href="https://hsp-iit.github.io/hemisphere-dataset-generation/">simulation environment</a> for synthetic data generation)</i>
  </p>
</p>

## Getting started
The project uses Unity 2021.3.28.f1. Find the version [here](https://unity3d.com/get-unity/download/archive) and click on the `Unity Hub` button to download.

## Installation
- Install [Git LFS](https://docs.github.com/en/repositories/working-with-files/managing-large-files/installing-git-large-file-storage). 
- Open a Command Prompt and run `git lfs install` to initialize it.<br>Then, clone the repository: `git clone --single-branch --branch main --recursive https://github.com/hsp-iit/hannes-wrist-control`
- Go on the Unity Hub, click on Open and locate the downloaded repository.
- This repository has a submodule located at `Python/libs/visual-servoing`, follow the instructions to set up the Python environment.

## Running the Simulation

This repository provides a simulation environment for the **Hannes prosthesis** in Unity. The wrist is controlled using **Image-Based Visual Servoing (IBVS)** from an eye-in-hand camera.

### Steps to Run
0. **Set up the Visual Servoing**

    Navigate to `Python/libs/visual-servoing/configs/conf.yaml` and set:
    ```bash
    vservo_name: IBVSFEPropPS
    lmbda_ps: 0.01
    ```

1. **Start the Python server**  
   ```bash
   cd <path_to_repo>/Python
   python src/socket/server/launch.py
   ```

2. **Open the Unity project** and press the **Play** button.

3. **Control the arm**  
   - Use `WASD`, `IJKL`, and the arrow keys to translate and rotate the arm in space.  
   - The wrist will automatically adjust to point at the target object.

### Implementation Details
Communication between Unity and Python is handled through a **TCP socket**:  
- Unity sends data (e.g., camera images, joint states, object poses) to the Python server.  
- The Python server computes the control action and sends it back to Unity.  

Key files:  
- Unity client: `Assets/Scripts/Socket/Client/SocketClient.cs`  
- Python server: `Python/src/socket/server/launch.py`

## Citation
If you find our work useful, please consider citing our paper as follows:
```
@inproceedings{vasile2025continuous,
  title={Continuous Wrist Control on the Hannes Prosthesis: a Vision-based Shared Autonomy Framework},
  author={Vasile, Federico and Maiettini, Elisa and Pasquale, Giulia and Boccardo, Nicol{\`o} and Natale, Lorenzo},
  booktitle={2025 IEEE International Conference on Robotics and Automation (ICRA)},
  pages={},
  year={2025},
}
```

## Mantainer
This repository is mantained by:
| | |
|:---:|:---:|
| [<img src="https://github.com/FedericoVasile1.png" width="40">](https://github.com/FedericoVasile1) | [@FedericoVasile1](https://github.com/FedericoVasile1) |
