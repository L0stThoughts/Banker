
# Banking Server Application Documentation

## Overview

The Banking Server application is a system that manages bank accounts, transactions, and client communications. It supports creating accounts, depositing, withdrawing, checking balances, and removing accounts, all handled via a command-based interface over a network connection. The application is started using `runbank.bat` and is accessed via PuTTY.

## Project Structure

The application consists of the following key components:

-   **Config.cs**: Manages application configuration, loading settings from `config.json`.
-   **BankData.cs**: Handles account storage, deposits, withdrawals, and persistence using JSON.
-   **BankServer.cs**: Manages TCP connections and client interactions.
-   **RequestHandler.cs**: Parses and processes client commands.
-   **Logging.cs**: Logs all the server responses.
-   **Program.cs**: Entry point of the application, initializing and starting the server.

## Configuration

Configuration settings are stored in `config.json` and can be modified using command-line arguments when running the program.

### Configurable Parameters:

-   **BankIp**: IP address of the bank server.
-   **Port**: TCP port for client connections.
-   **StorageFile**: File path where account data is stored (`bankdata.json`).
-   **ClientTimeoutMs**: Timeout for client connections (in milliseconds).
-   **ProxyTimeoutMs**: Timeout for proxy requests to other banks.

## Building the Application

Before running the server, you need to build the application. Follow these steps:

1. Open a terminal or command prompt in the project directory.
2. Restore the required dependencies using NuGet:
   ```sh
   dotnet restore
   ```
3. Build the application using:
   ```sh
   dotnet build
   ```

## Running the Application

### 1. Starting the Server

Run the `runbank.bat` file to start the bank server.

### 2. Connecting with PuTTY

1.  Open **PuTTY**.
2.  Enter the **IP address** and **port** of the bank server.
3.  Set **Connection type** to `Raw`.
4.  Click **Open** to connect.

## Command Interface

Clients interact with the server using text-based commands. Commands should be entered in the PuTTY terminal after connecting to the server.

### Available Commands

Command

Description

`BC`

Returns the bank's IP address.

`AC`

Creates a new account.

`AD <acct>/<ip> <amount>`

Deposits money into an account.

`AW <acct>/<ip> <amount>`

Withdraws money from an account.

`AB <acct>/<ip>`

Gets the balance of an account.

`AR <acct>/<ip>`

Removes an account (if balance is 0).

`BA`

Returns the total bank balance.

`BN`

Returns the number of accounts.

`exit`

Closes the connection.

`help` or `?`

Displays available commands.

## Data Storage

Account data is stored in a JSON file (`bankdata.json`). The format is as follows:

```json
{
    "10001": 5000,
    "10002": 12000
}
```

Each key represents an account number, and the value represents the account balance.

## Proxying Requests

If a request is made for an account at a different bank, the command is forwarded to the respective bank IP using `ProxyToRemote()`.

## Stopping the Server

Press **ENTER** in the console where the server is running to stop the server gracefully.

## Logging

Logs are written to the console and can be extended to a file if needed.

## Conclusion

This banking server provides a simple and extensible system for handling basic banking operations over a TCP network. The architecture allows for easy modifications and scalability by integrating additional banks or services.


