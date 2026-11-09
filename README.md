<img width="930" height="210" alt="Workflow" src="https://github.com/user-attachments/assets/916020de-ccde-42d3-bce0-807b2abf1237" />
What is RaceDay?

RaceDay is a website project designed to help people in South Africa manage running, walking, and cycling events. Instead of using paper or messy spreadsheets, event organisers can use this system to create events, and participants can use it to enter races and track their results.

System Roles

Our system has two user roles:

Organiser: Can create and edit events, set up race categories, and record finish times and positions for participants.

Participant: Can register an account, browse upcoming races, enter an event, and view their own race history.

Documentation

All planning documents for Part 1 are inside the /docs folder:

ERD: Shows our database tables and how they link together.

API Endpoint Plan: A table showing all the web routes our system will use.

SQL Script: The database code to create tables and add sample data.

CI/CD Validation

We use GitHub Actions to check that our documentation folder and files are correct automatically.

Part 3 added BannerImageUrl to Event and ProfilePictureUrl to User

# RaceDay - Full Event Management Platform

## Docker Run Command
To build and run the MVC web application in Docker:
```bash
docker build -t raceday-web -f RaceDay.Web/Dockerfile .
docker run -p 8080:80 raceday-web
```
Open `http://localhost:8080` in your browser.

## Database Schema Note
Part 3 added `BannerImageUrl` to the `Event` table and `ProfilePictureUrl` to the `User` table to support Azure Blob Storage.
