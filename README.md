# OpenAICustomFunctionCallingAPI\

This is a custom API designed to simplify consumption of the OpenAI endpoint, and create a methodology for connecting a virtually limitless amount of functions to call to a single request using the OpenAI service's function calling feature.

This is accomplished by using a single function definition which then calls more specialized configurations that can gather additional data from the user if needed, and contain a larger variety of function definitions. This reduces costs by only providing function definitions when necessary (which consume tokens), and through triaging, can introduce as many functions as needed.

Please add your own API token in appsettings.json after cloning the repo.

Future Features (Descending Priority):
- Provide conversation history from the client?
- Document upload/extraction support
- Real time internet search
- Speech support
- Create a library of default AI profiles
- Tool calls for reading/writing to a RAG database (library of default tools)

Technical Debt Items (Descending Priority):
- Messages in the MessageHistory repository should be hashed before storage (Is there a more secure storage method?)
- Revisit how default values are applied in the streaming hub
- Rename norms "Magnitudes" (requires SQL column drop) (or just drop this column entirely?)
- Set dimensions of vector embedding requests based off of the model
- remove "DTO" from DTO names
- normalize DTO names for RAG indexing
- Change "chatstream" to "chat/socket" for SignalR hub
- revisit asynchronous design, particularly as it pertains to streaming
- Use a more specialized character than commas to singnal the end of a stop sequence (possibly use recuring characters of a certain kind, like "@@@@@")
- Ensure program.cs service lifteimes are properly created
- Implement pagination for GenericRepository.GetAllAsync()
- Add documentation to classes
- Configure launchsettings.json for both a prod and dev environment
- Consolidate system tool strings in an appsettings section

Refactoring Items (Descending Priority):
- validation handler
- error messaging handler
- Add logic to switch to fallback resources during service outages (More than likely should be possible by setting polly settings)
- support for alternative AI APIs (just groq.com to start)
- Remove camelcasecontract resolver and add JsonPropertyName for ALL properties instead
- Use cascade to potentially simplify some operations involving associative tables
- Modify the messageHistory tables so that they can be derived and utilized (like the RAG Database DTOs and their associated classes)
- provide default values to query requests? Or get these from the database?
- perform string cleaning on any AGI responses from system model profiles
- Combine embedding and AGI clients?
- revisit route design to increase RESTfullness
- Create associate tables to store vector embeddings rather than only storing their norms
- Convert DTO methods to mapper/converter classes
- simplify completionLogic.cs possibly using interfaces to decouple from the client layer wherever possible
- Clean up method complexity in all business logic classes (move streaming logic possibly)
- flatten ChatRequestDTO
- return function calling information the same way its being returned via streaming
- Create more streaming methods for when values should be retrieved from appsettings
- Add parameter for preventing a conversation from being saved (something to signify its a completion vs chat request?)
- API controller route for returning streaming via server side events
- Rethink how usernames are used, especially with conversation history (remove username column from database?)
- Completely Rearchitect the sql database, and associated DAL (Potentially add Id column for all tables and use generics wherever possible) (Only use generic map from reader methods) (also create a new database)
- see if you can create a streaming client that is service agnostic similar to the AI Client for REST operations (Doesn't seem like it should be a hight priority at the moment)