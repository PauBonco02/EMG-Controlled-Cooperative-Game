# EMG-Controlled-Cooperative-Game
This repository provides the unity files for **EMG Quest**: an 2d cooperative game controlled via EMG signals using **Cometa Wireless EMG Devices**. This project was developed by students from the [Yoshimura Lab](https://www.nicep.first.iir.titech.ac.jp/ylab/) at the [Institute of Science Tokyo](https://www.isct.ac.jp/en).
![EMG QUEST](https://github.com/user-attachments/assets/71a38879-b225-48b7-a764-b12033cb09f2)

## Included in this project:
- Plugins from the **Cometa SDK** for receiving EMG data.
- Scripts for **signal processing**, **calibration**, and **mapping EMG signals to gameplay actions**.
- Full source code and assets for the game itself.

## EMG Quest: Game and Controls
EMG Quest is just a short prototype created to test the use of EMG in unity for game control. It is a 2D cooperative platformer game with 4 levels in which each player controls a square. With two sensors in the forearm per player, they can trigger 3 different actions. Considering the right arm is used:
- **Flexing** the wrist to the left: **Move left**
- **Extending** the wrist to the right: **Move right**
- **Activating** both flexor and extensor muscles simultaneously: **Jump**
To advance to the next level, both characters must be in the door with the same color as them at the same time. Some mechanics include using the other player like a platform and colored tiles only solid for the player with the same color.
![GameRecordingTrimmed](https://github.com/user-attachments/assets/c1457c91-10f3-4768-84ca-dd49f00fe2e8)

##EMG sensors and data processing
