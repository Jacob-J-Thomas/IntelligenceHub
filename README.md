# OpenAICustomFunctionCallingAPI\

This is a custom API designed to simplify consumption of the OpenAI endpoint, and create a methodology for connecting a virtually limitless amount of functions to call to a single request using the OpenAI service's function calling feature.

This is accomplished by using a single function definition which then calls more specialized configurations that can gather additional data from the user if needed, and contain a larger variety of function definitions. This reduces costs by only providing function definitions when necessary (which consume tokens), and through triaging, can introduce as many functions as needed.

Please add your own API token in appsettings.json after cloning the repo.

Future Features (Descending Priority):
- RAG database support
- Add route profile/associate/{name}
- Add route profile/dissociate/{name}
- Add route profile/get/{name}/tools
- Controller for interacting with saved conversations
- Some kind of methodology for providing the entire conversation history from the client and/or disabling saving of chat history
- Add logic to switch to fallback resources during service outages
- Document upload/extraction support
- Real time internet search
- Speech support
- Tool calls for reading/writing to a RAG database
- Create a library of default AI profiles

Technical Debt Items (Descending Priority):
- revisit asynchronous design, particularly as it pertains to streaming
- Use a more specialized character than commas to singnal the end of a stop sequence (possibly use recuring characters of a certain kind, like "@@@@@")
- Ensure program.cs service lifteimes are properly created
- Implement pagination for GenericRepository.GetAllAsync()
- Add documentation to classes
- Configure launchsettings.json for both a prod and dev environment
- Move system tool strings to an appsettings section

Below should be accomplished before raising your next PR

Refactoring Items (Descending Priority): 
- Implement this as another meta function (similar to reference_calls): https://community.openai.com/t/emulated-multi-function-calls-within-one-request/269582
- API controller route for returning streaming via server side events
- support for alternative AI APIs (just groq.com to start)
- see if you can create a streaming client that is service agnostic similar to the AI Client for REST operations
- Add parameter for preventing a conversation from being saved (something to signify its a completion vs chat request?)

- Clean up completionLogic (move streaming logic possibly)
- Remove stream from database, assign depending on the request type instead
- Rearchitect DTOs emphasizing reusibility (particularly a few newer ones related to OpenAI API response deserialization) (also flatten modifiers in the ChatRequestDTO somehow, (maybe make a boolean for "modified")
- Modify Streaming to support any client
- Reafactor AI Client to accept any client and seperate stream into another class (OpenAIStreamClient.cs). Maybe rename AIClient GenericAIClient
- Completely Rearchitect the sql database, and associated DAL (Potentially add Id column for all tables and use generics wherever possible) (Only use generic map from reader methods) (also create a new database)
- Rethink how usernames are used, especially with conversation history (remove username column from database?)
- Clean up a few methods in Profile and Tool logic/DAL, particularly as it pertains to some data retrieval operations

