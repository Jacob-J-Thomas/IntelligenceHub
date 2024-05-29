# OpenAICustomFunctionCallingAPI\

This is a custom API designed to simplify consumption of the OpenAI endpoint, and create a methodology for connecting a virtually limitless amount of functions to call to a single request using the OpenAI service's function calling feature.

This is accomplished by using a single function definition which then calls more specialized configurations that can gather additional data from the user if needed, and contain a larger variety of function definitions. This reduces costs by only providing function definitions when necessary (which consume tokens), and through triaging, can introduce as many functions as needed.

Please add your own API token in appsettings.json after cloning the repo.

Future Features (Descending Priority):
- RAG database support
- support for alternative AI APIs (just groq.com to start)
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
- Change "chatstream" to "chat/socket" for SignalR hub
- revisit asynchronous design, particularly as it pertains to streaming
- Use a more specialized character than commas to singnal the end of a stop sequence (possibly use recuring characters of a certain kind, like "@@@@@")
- Ensure program.cs service lifteimes are properly created
- Implement pagination for GenericRepository.GetAllAsync()
- Add documentation to classes
- Configure launchsettings.json for both a prod and dev environment
- Move system tool strings to an appsettings section

Refactoring Items (Descending Priority):
- instead of using constructors use the methods you built for the constructors
- simplify completionLogic.cs possibly using interfaces
- Clean up method complexity in all business logic classes (move streaming logic possibly)
- flatten ChatRequestDTO
- return function calling information the same way its being returned via streaming
- Create more streaming methods for when values should be retrieved from appsettings
- Add parameter for preventing a conversation from being saved (something to signify its a completion vs chat request?)
- API controller route for returning streaming via server side events
- Rethink how usernames are used, especially with conversation history (remove username column from database?)
- Completely Rearchitect the sql database, and associated DAL (Potentially add Id column for all tables and use generics wherever possible) (Only use generic map from reader methods) (also create a new database)
- see if you can create a streaming client that is service agnostic similar to the AI Client for REST operations (Doesn't seem like it should be a hight priority at the moment)