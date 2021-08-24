# PrintTool

Hello, this is the guide and readme for the program "PrintTool" created by derek hearst.
This program is undergoing heavy development and can / will change as features that are needed arise.
---------------------------------------------------------------------------
CURRENT CAPABILITES / PLANNED

CURRENT
1. Connecting and gathering logs - only serial and telnet at the moment
2. Updating firmware - jolt, sirus, and dune
3. Sending various print jobs - using the "IPP" technology, USB, and raw 9100
4. Status feedback and rich communication with the printer
5. Auto Upddating

PLANNED
1. Capturing machine logs
2. Working with HP UPD Directly
3. Capability to work with finishers
4. Automated tests
5. CLI / Linux 
---------------------------------------------------------------------------
INSTALLATION

1. Run and install the included windows desktop runtime located in the share
2. After installtion has completed, run PrintTool.exe
3. The program should auto install itself into your user directory and then open the folder it installed into
4. From there you are free to create a shortcut or add it to your taskbar
---------------------------------------------------------------------------

---------------------------------------------------------------------------
USING THE TOOL

Device and Log Setup
1. You are able to save and load frequently used connections to printers, with model details and connection options
2. After that you are able to start a connection to the printer and start logging serial/telnet connections

Firmware
1. There are three tabs to select from, Dune and Sirus are the most feature complete, but JOLT is functional
2. The program will automatically select the latest firmware, but you can manually change what version or distrobution to select
3. The program supports both links to firmware, and folders on your pc / remote drives
4. On the right side of firmware, there is a "Quick Links" group, with frequently used firmware locations

Print
1. Currently only able to send 9100 and USB, with PJL as the descriptor lang.
2. You can save print jobs and send any other job if you put it in "Data\Jobs\" directory of the program.
