# OpenAICustomFunctionCallingAPI\

This is a custom API designed to simplify consumption of the OpenAI endpoint, and create a methodology for connecting a virtually limitless amount of functions to call to a single request using the OpenAI service's function calling feature.

This is accomplished by using a single function definition which then calls more specialized configurations that can gather additional data from the user if needed, and contain a larger variety of function definitions. This reduces costs by only providing function definitions when necessary (which consume tokens), and through triaging, can introduce as many functions as needed.

Please add your own API token in appsettings.json after cloning the repo.


Future Features:
- Add route profile/associate/{name}
- Add route profile/dissociate/{name}
- Add route profile/get/{name}/tools
- Conversation history via id
- Add logic to switch to fallback resources during service outages
- Self writing database
- RAG database support
- Support for streaming
- Speech support
- Document input/extraction support
- Real time internet search support

Technical Debt Items (Descending Priority):
- Go through 500 status codes for _serverSidesStatusCode list
- Clean up 
- Clean up DTOs, particularly a few newer ones related to OpenAI API response deserialization
- Clean up a few methods in Profile and Tool logic/DAL, particularly as it pertains to some data retrieval operations
- Delete unused files/classes
- Add additional properties for Tools (Name, Role, Function details). This will assist with dialogues between AI models
- Ensure program.cs service lifteimes are properly created as well as cors policy