# Uwp Is Dead
https://developercommunity.visualstudio.com/t/Add-NET-678-support-to-UWP/1596483

# UwpHmiToolkit
A toolkit for UWP apps as a HMI (Human Machine Interface) or a client/server between industrial machines.

Try to provide an easy way to remote control machines with performance consideration, for technical engineers or solution developer in industrial.

## Protocols to implement (Plan)
**Principles**:  Use common, trendy protocols, try to cover most of scenario.
- MC Protocol (abbreviation of MELSEC&trade; communication protocol Develop by Mitsubishi&trade;, cover most machine in Asia, and I live in Taiwan) 
- Modbus&trade; (Most common, but old...and low-performance for now)
- EtherNet/IP™ (Develop by OPCUA&reg;)
- PROFINET™ (Optional, Because I don't have experience on this yet.)
- SECS/GEM (A standard for semiconductor industrial)

### Sequence of develop (Normally):
- Needs of my work cases 
(I will start on McProtocol, and then SEC/GEM)
- Transmission Protocol: UDP->TCP
- Data Code: Binary->ASCII
- Role: Client->Server

## Believes and why
- Language of machines is free, just like human language.
- Emancipating engineers then they can put more time on system logic, correctness of database structure and GUI.
- Get cost lower on studying protocol documents, then many web developer can entry industrial of automation.
- Give a "small" reason let Windows IoT Core live.


## Btw
 I still learning to handle software development skills, you will discover my codes is not match to many paradigms of this ecosystem.
 
 My background is a programmer for PLCs, HMI developer of machine tools and designs some low-voltage circuit,
 This is my first opensource project (Except of "HELLO WORLD!") and if you have ideas or my coding skill is too bad and that drivings you to give me some advice, or you want to join, please tell me. :pray:
