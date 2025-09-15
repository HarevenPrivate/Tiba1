Project: ItemRepositoryAPI & Worker Service
 Docker Setup
 Prerequisites
 • 
• 
• 
Docker Desktop installed and running.
 .NET 9 SDK installed (for local builds if needed).
 Optional: Visual Studio 2022.
 Directory Structure
 Tiba/
 ├── IteamRepositoryAPI/
 │   ├── Dockerfile
 │   └── ...
 ├── ItemRepositoryWorkerService/
 │   ├── Dockerfile
 │   └── ...
 ├── docker-compose.yml
 └── README.txt
 Setup & Run Locally Using Docker
 1. 
2. 
Open PowerShell in the project root (
 Build Docker images:
 docker-compose build
 Tiba folder).
 3. 
4. 
Start services:
 docker-compose up-d
 Check running containers:
 docker ps
 1
You should see 
iteamrepositoryapi , 
itemrepositoryworkerservice , 
postgres , and 
rabbitmq running. 
Verify API is reachable:
 Open a browser or use curl: 
curl http://localhost:5000/swagger
 You should see the Swagger UI.
 Access RabbitMQ Management UI:
 Open a browser: 
http://localhost:15672
 Default credentials: 
guest / 
Database Details:
 guest
 PostgreSQL is exposed on 
localhost:5432
 DB name: 
ItemDB
 User: 
postgres
 Password: 
Noya@27189
 Stop Services
 docker-compose down
 Notes

Migrations:
 ItemRepositoryWorkerService automatically runs EF Core migrations on start. If
 the DB already exists, it will not re-create tables.
 Logs:
 docker logs-f iteamrepositoryapi
 Debug: Use Visual Studio 2022 to attach to Docker containers if needed.
 Ensure 
docker-compose.yml and Dockerfiles are not deleted or modified unexpectedly
