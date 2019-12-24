# DR2Rallymaster
## A results processor and exporter for Dirt Rally 2.0 Clubs

### Features
This program extends Dirt Rally 2.0 Clubs results with several features:

* Export results to CSV
  * Stage results
  * Overall results per stage
  * Event statistics
  * Chart data to create position charts in Excel or Google Sheets
  
* Detection and removal of DNF drivers in cases where the driver has a PC crash, or uses Alt+F4 to continue driving in the rally when they had terminal damage

* View metadata stored in RaceNet that is not displayed on the Clubs site

* Selectable themes and colors - hit the gear icon in the top left corner to change the base theme and accent color

### How to use
1) Log into Racenet using the "log into racenet" button
2) Wait for authentication to be setup, the authentication screen should be closed automatically
3) Search for the Club using the Club ID, these are the digits at the end of the club URL - Example: https://dirtrally2.com/clubs/club/183582 is Club ID: 183582
4) In the left column, all championships are displayed. Select a championship to see the metadata and display events for that championship
5) In the middle column, events are displayed when a championship is selected. Select an event to see the event metadata, and to enable results exporting
6) Select the "export to csv" button when an event is selected to export the stage data to csv

### Notes
Codemasters has locked part of the API behind authentication. The app will display the Racenet login screen and use the authentication tokens in the API calls. No login data is stored beyond the cookies set by the Racenet login process.

I have not tested any of the third party authentication methods, only the standard Racenet email authentication.
