# ▶️ How to Run the Application

This guide explains how to get the Banking Transactions API up and running on your local machine.

## Option 1: Running with .NET SDK (If installed)

If you have the **.NET 10 SDK** installed on your system, you can run the API natively using the CLI.

1. Open your terminal and navigate to the project directory:
   ```bash
   cd src/BankingApi
   ```
2. Run the application:
   ```bash
   dotnet run
   ```
3. The API will start and display the URL it is listening on (typically `http://localhost:5000`).

---

## Option 2: Running via Docker (No .NET installation required)

If you **do not** have .NET installed, you can easily run the application using **Docker**. I have provided a `Dockerfile` specifically for this purpose!

### Prerequisites:
- You must have [Docker](https://docs.docker.com/get-docker/) installed and running.

### Steps:
1. Open your terminal and navigate to the directory containing the source code:
   ```bash
   cd src/BankingApi
   ```
2. Build the Docker image (this will safely download the required .NET environment entirely inside the container):
   ```bash
   docker build -t banking-api .
   ```
3. Run the container, exposing the API on port `8080` on your machine:
   ```bash
   docker run -d -p 8080:8080 --name my-banking-api banking-api
   ```
4. **Success!** The API is now running entirely independently of your local system at `http://localhost:8080`.

> **Note**: To stop the container when you are finished, run:
> `docker stop my-banking-api && docker rm my-banking-api`

---

## 🧪 How to Test the API

Regardless of which method you chose to run the API, you can test it easily using the provided test suite.

1. Open the file `demo/api-tests.http` in an editor like **Visual Studio Code** (requires the *REST Client* extension) or **JetBrains Rider**.
2. **If you used Option 1 (.NET CLI)**: Ensure the `@baseUrl` variable at the top of the file matches your terminal output (e.g., `http://localhost:5000`).
3. **If you used Option 2 (Docker)**: Change the `@baseUrl` variable at the top of the file to `http://localhost:8080`.
4. Click "Send Request" above each block in the file to securely interact with the API endpoints!