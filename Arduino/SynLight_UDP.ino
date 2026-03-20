#define FASTLED_ALLOW_INTERRUPTS 1
#define FASTLED_INTERRUPT_RETRY_COUNT 1

#include <Wire.h>
#include <FastLED.h>
#include <WiFi.h>
#include <WiFiUDP.h>

#define NUM_LEDS 514
#define DATA_PIN 8
#define UDP_TX_PACKET_MAX_SIZE 1200

const char* ssid = "Synless_Wifi";
const char* password = "";

IPAddress local_IP(192, 168, 8, 134);
IPAddress gateway(192, 168, 8, 1);
IPAddress subnet(255, 255, 255, 0);

const unsigned int localPort = 8787;

CRGB leds[NUM_LEDS];
WiFiUDP UDP;

static uint8_t packetBuffer[UDP_TX_PACKET_MAX_SIZE];
const char ReplyBuffer[] = "pong";

unsigned long lastUdpTime = 0;
bool active = true;

int totalLedCounter = 0;
bool frameReady = false;

unsigned long frameCount = 0;

void fillRange(int start, int end, int r, int g, int b)
{
    for (int n = start; n < NUM_LEDS && n < end; n++)
    {
        leds[n] = CRGB(r, (int)(g * 0.85), (b * 3) >> 2);
    }
}

void setup()
{
    Serial.begin(115200);
    delay(500);

    FastLED.clear(true);
    FastLED.addLeds<NEOPIXEL, DATA_PIN>(leds, NUM_LEDS);
    FastLED.setBrightness(150);
    FastLED.setMaxPowerInVoltsAndMilliamps(5, 4000);

    WiFi.config(local_IP, gateway, subnet);
    WiFi.begin(ssid, password);

    while (WiFi.status() != WL_CONNECTED)
    {
        delay(100);
    }

    UDP.begin(localPort);

    lastUdpTime = millis();
}

void processPacket(int len)
{
    if (len <= 0) return;

    uint8_t type = packetBuffer[0];

    if (type == 0 && len == 5 && packetBuffer[1] == 'p' && packetBuffer[2] == 'i' && packetBuffer[3] == 'n' && packetBuffer[4] == 'g')
    {
      Serial.printf("PING from %s:%d\n", UDP.remoteIP().toString().c_str(),  UDP.remotePort());
      UDP.beginPacket(UDP.remoteIP(), UDP.remotePort());
      UDP.write((const uint8_t*)ReplyBuffer, strlen(ReplyBuffer));
      UDP.endPacket();
      return;
   }

    if (type == 2 || type == 3)
    {
        int ledNb = (len - 1) / 3;
        int processed = 0;

        for (int i = 0; i < ledNb; i++)
        {
            int offset = 1 + i * 3;
            if (offset + 2 >= len) break;

            int ledIndex = totalLedCounter + i;
            if (ledIndex >= NUM_LEDS) break;

            leds[ledIndex] = CRGB(
                packetBuffer[offset],
                (int)(packetBuffer[offset + 1] * 0.85),
                (packetBuffer[offset + 2] * 3) >> 2
            );

            processed++;
        }

        totalLedCounter += processed;

        if (type == 3)
        {
            frameReady = true;
            totalLedCounter = 0;
        }
    }

    lastUdpTime = millis();
    active = true;
}

void loop()
{
    int packetSize;

    while ((packetSize = UDP.parsePacket()) > 0)
    {
        if (packetSize > UDP_TX_PACKET_MAX_SIZE)
        {
            UDP.flush();
            continue;
        }

        int len = UDP.read(packetBuffer, UDP_TX_PACKET_MAX_SIZE);
        processPacket(len);
    }

    if (frameReady)
    {
        FastLED.show();
        frameReady = false;

        frameCount++;
        Serial.println(frameCount);
    }

    if ((millis() - lastUdpTime > 5000) && active)
    {
        active = false;
        lastUdpTime = millis();

        fillRange(0, NUM_LEDS, 0, 0, 0);
        FastLED.show();
    }

    yield();
}