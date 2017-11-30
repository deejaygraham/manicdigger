#import the minecraft.py module from the minecraft directory
import mcpi.minecraft as minecraft
import time

time.sleep(2)

# Connect to minecraft by creating the minecraft object
world = minecraft.Minecraft.create()
count = 0

x = 0
y = 0
z = 0

while True:
    count += 1

    block = world.getBlock(x, y, z)

    print("block = " + str(block))

    world.saveCheckpoint()
    world.postToChat("Hello, Minecraft World " + str(count))
    time.sleep(1)
    world.restoreCheckpoint()

    block += 1
    world.setBlock(x, y, z, block)
