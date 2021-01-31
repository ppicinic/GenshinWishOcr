# GenshinWishOcr
Ocr a image sequence for genshin wish history into a csv

## Running the bot
Open solution with Visual Studio 2019 and build the solution. You will need .netcore3.1.

Run the exe with the following commandline parameters:

GenshinWishOcr.exe PngSequenceFolder outputfilename.csv multithreadPercentage

multithreadPercentage is optional and should be a double value that will utilize a % of your device's cpu's cores. Full utilization is probably most optimal with 1.5 but you may use .75 for to leave a little bit of your CPU untaxed.

Some example usages

GenshinWishOcr.exe C:/MyWishImageSequence/ C:/mywishes.csv

GenshinWishOcr.exe C:/MyWishImageSequence/ C:/mywishes.csv .75

GenshinWishOcr.exe C:/MyWishImageSequence/ C:/mywishes.csv 1.5
