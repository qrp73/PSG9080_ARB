# PSG9080_ARB
Arbitrary wave download/upload command line utility for PSG9080 signal generator

![PSG9080](https://github.com/qrp73/PSG9080_ARB/assets/46676744/a4469419-fb0c-42e6-ac15-44c7114c8b17)


## Install

Installation and configuration is not required, just unzip archive and run it.

## Usage

1) Connect PSG9080 signal generator to USB and power on
2) In case of needs install CH340 drivers
3) Run command line tool

### Download arbitrary wave number 1 from a PSG9080 to the file:

PSG9080_ARB.exe COM3 -read 1 wave01.txt

### Upload arbitrary wave number 1 from a file to the PSG9080:

PSG9080_ARB.exe COM3 -write 1 wave01.txt

where:

COM3 - usb/serial port of PSG9080 signal generator (you can find it on the system Device Manager under "Ports (COM and LPT) entry")

1 - arbitrary wave number

wave01.txt - text filename to save or load arbitrary wave (it should consists of 8192 lines with decimal values)


# How to run on Linux

In order to run the tool on Linux OS, you're needs to install mono. Then you can run the tool the same as on Windows, just att "mono" before command. 

For example:
```
mono PSG9080_ARB.exe COM3 -read 1 wave01.txt
```
```
mono PSG9080_ARB.exe COM3 -write 1 wave01.txt
```


### Note

read and write commands upload and download wave as is in 14 bit integer format. The file should consists of 8192 text lines with text representation of number from 0 to 16383

Original software use a little different format. It strore values in 16 bit integer format and convert it during upload procedure. Such conversion don't allows user to control data precisely. This is why I'm using 14 bit data format by default. My tool upload data as is with no conversion and no scale.

In order to support arbitrary wave files from original software I added two additional commands: read16 and write16.

### Download arbitrary wave number 1 from a PSG9080 to the file in 16-bit format:

PSG9080_ARB.exe COM3 -read16 1 wave01.txt

### Upload arbitrary wave number 1 from a file in 16-bit format to the PSG9080:

PSG9080_ARB.exe COM3 -write16 1 wave01.txt

### Remarks

The source code also includes the code for the firmware upgrade command, but this command is disabled in the source code and you will be unable to use it by default, because I never tested it on the real device. It was tested on simulator and I don't know if it works on the real device. Do not use this command because upgrage command may fails and you can brick your device. 
