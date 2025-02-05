# CosmosDB Migration Tool

Welcome to the **CosmosDB Migration Tool**! This application is designed to help you manage and update data in your Azure CosmosDB database, specifically targeting chat session data.

## Features

- **Update Chat Session Titles**: Automatically updates chat session titles based on the first message in the session.
- **Interactive Menu**: Provides a simple and interactive console-based menu for managing operations.

## Prerequisites

To use this tool, ensure you have the following:

1. **Azure CosmosDB Account**:
   - Connection string
   - Database name
   - Container names for chat sessions and chat messages
2. **.NET 6 SDK** installed on your machine.
3. **Configuration File**:
   - Create `appsettings.json` in the application's root directory with the following structure:

```json
{
  "CosmosDB": {
    "ConnectionString": "<your-cosmosdb-connection-string>",
    "DatabaseName": "<your-database-name>",
    "ChatSessionContainerName": "<chat-session-container-name>",
    "ChatMessageContainerName": "<chat-message-container-name>"
  }
}
```

4. **Optional**: An `appsettings.Development.json` file for overriding settings during development.

## Getting Started

1. **Clone the Repository**:
   Clone the repository containing this application to your local machine.

2. **Set Up Configuration**:
   Add your CosmosDB connection details to the `appsettings.json` file.

3. **Build the Application**:
   Run the following command in the root directory to restore dependencies and build the project:

   ```bash
   dotnet build
   ```

4. **Run the Application**:
   Start the console application with:

   ```bash
   dotnet run
   ```

## Using the Tool

When you run the application, you'll be greeted with an interactive menu:

1. **Update Chat Session Titles**: Select this option to update chat session titles based on the first message in each session. Titles starting with `Copilot @` will be replaced with the content of the session's first message.
2. **Exit**: Quit the application.

### Example Workflow

1. Choose option `1` to update chat session titles.
2. The application will query the chat session container for titles matching the pattern `Copilot @`.
3. For each matching session, the first message's content will replace the existing title.
4. Updated sessions will be saved back to the container, and the changes will be logged in the console.

## Project Structure

- **Program.cs**: The main entry point of the application.
- **CosmosDB Initialization**:
  - Establishes a CosmosDB client connection.
  - Configures access to specified containers.
- **Chat Session Update Logic**:
  - Queries chat sessions.
  - Updates titles using the first message content.
  - Performs upsert operations to save changes.

## Assumptions

- This tool assumes specific schema attributes for chat sessions (`id`, `title`) and chat messages (`chatId`, `authorRole`, `content`, `_ts`).

## Troubleshooting

- **Missing Configuration**: Ensure `appsettings.json` is correctly set up.
- **Permission Issues**: Verify that your CosmosDB connection string has the necessary permissions.
- **Performance**: Large datasets may take time to process.

## License

This project is licensed under the MIT License. See the `LICENSE` file for details.

---

Thank you for using the CosmosDB Migration Tool! For questions or issues, feel free to reach out or open an issue in the repository.

