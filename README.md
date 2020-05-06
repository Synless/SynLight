# SynLight

**[Ambient lighting system](https://en.wikipedia.org/wiki/Ambilight) for Windows**

![](https://raw.githubusercontent.com/Synless/SynLight/master/SynLight/Images/demo2.png)



This software on the computer side processes screenshots and sends data to a ESP8266/NodeMCU microcontroller via Wifi (UDP). The microcontroller then drive individually addressable RGB LEDs on the rear of the screen, to match the color of its edges. This allows for a nice experience while watching movies, but also while working on a daily basis, by limiting visual fatigue.

**See the [Wiki](https://github.com/Synless/SynLight/wiki) for more informations.**

This software can account for the absence of LEDs in the corners, the first LED not being in a corner, and the black bars in 21:9 content :

![](https://raw.githubusercontent.com/Synless/SynLight/master/SynLight/Images/small_Explanation.png)
