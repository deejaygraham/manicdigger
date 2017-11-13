#import the minecraft.py module from the minecraft directory
import mcpi.minecraft as minecraft
import time

time.sleep(2)

# Connect to minecraft by creating the minecraft object
world = minecraft.Minecraft.create()
world.postToChat("Hello, Minecraft World")
