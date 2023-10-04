# Machine Learning-based Countermeasures to Mislead Hostile Swarm Missions

## Overview
This repository contains the supplementary code for a paper and a master's thesis.
The code includes simulations of agents, machine learning components, and tools for evaluating the results using Jupyter notebooks.

## Getting Started
This product was developed and tested under Ubuntu 23.04. It utilizes [Unity](https://unity.com/download) as well as [ML-Agents](https://unity-technologies.github.io/ml-agents/)

### Unity Project
This project uses Unity version 2021.3.16f1 (check [./ProjectSettings/ProjectVersion.txt](./ProjectSettings/ProjectVersion.txt)).
There exists defenders and attackers for Gray Wolf Optimizer (GWO) and Slime Mould Algorithm (SMA) in [./Assets/GWO](./Assets/GWO) and [./Assets/SlimeMould](./Assets/SlimeMould).
The defender that defend GWO and SMA in combination can be found under [./Assets/MergeTrainer](./Assets/MergeTrainer).

### Python Part
For analyzing the produced data by the Unity part Python is used.
The Python code utilizes a standard [pip-sync](https://github.com/jazzband/pip-tools/blob/main/README.md) setup.
The Python Version is specified in [./python_part/.python-version](./python_part/.python-version).
The code can be found in the [./python_part](./python_part)` folder.
Data evaluation is done in [ConvexHullAnalysis.ipynb](./python_part/ConvexHullAnalysis.ipynb). 
As the Unity part produces JSONL (JSON line seperated) files there is [jsonl2sqlite.py](./python_part/jsonl2sqlite.py) to convert them into SQLite databases. This way more tooling can work with the data directly.  

## Additional Information
This project serves as supplementary material for a paper and a master's thesis.
Currently, there are no plans for external contributions or collaborations on this project.
