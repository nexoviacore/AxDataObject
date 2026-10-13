using ARMCommon.Helpers;
using ARMCommon.Interface;
using ARMCommon.Model;
using Dapper;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Npgsql;
using NPOI.SS.Formula.Functions;
using OpenAI;
using OpenAI.Chat;
using OpenAI.Models;
using StackExchange.Redis;
using System.Data;
using System.Diagnostics.Eventing.Reader;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using Twilio.TwiML.Voice;

namespace ARM_APIs.Controllers
{
    [Route("")]
    [ApiController]
    public class AxpertAIController : ControllerBase
    {
        private readonly IConfiguration _configuration;
        private readonly Utils _common;
        


        public AxpertAIController(IConfiguration configuration, Utils common)
        {
            _configuration = configuration;
            _common = common;
           
        }

        public class ARMAI
        {
            public string prompt { get; set; }  
        }

        public class ARMAIForm
        {
            public string prompt { get; set; }
            public string formname { get; set; }
        }



        [HttpPost("api/v1/AIChat")]
        public async Task<IActionResult> AIChat(ARMAI input)
        {
            string key = "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";
            string connectionString = "Host=172.16.0.135; Database=demoagileconnect; Username=demoagileconnect; Password=log";
            ChatClient client = new(model: "gpt-4o", apiKey: key);

            ChatCompletion completion = await client.CompleteChatAsync(input.prompt);

            var contentList = completion.Content;
            if (contentList != null && contentList.Count > 0)
            {
                var firstContent = contentList[0]?.Text;

                if (!string.IsNullOrEmpty(firstContent))
                {

                    var matches = Regex.Matches(firstContent, @"```([\s\S]*?)```");

                    if (matches.Count > 0)
                    {
                        List<string> extractedContents = new List<string>();

                        foreach (System.Text.RegularExpressions.Match match in matches)
                        {
                            string extractedContent = match.Groups[1].Value.Trim();

                            string cleanedContent = Regex.Replace(extractedContent, @"\bsql\b", string.Empty, RegexOptions.IgnoreCase);

                            extractedContents.Add(cleanedContent);
                        }

                        return Ok(new
                        {
                            extractedContents,

                        });
                    }
                    else
                    {
                        return Ok(new { success = false, message = "No content found within triple backticks." });
                    }
                }
                else
                {
                    return Ok(new { success = false, message = "Text content is empty or null." });
                }
            }
            else
            {
                return Ok(new { success = false, message = "No content found in the completion response." });
            }
        }
                                    
        [HttpPost("api/v1/AIChatImport")]
        public async Task<IActionResult> AIChatImport(ARMAI input)
        {
            if (input == null || string.IsNullOrEmpty(input.prompt))
            {
                return BadRequest(new { success = false, message = "Input prompt is required." });
            }

            string key = "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";
            string connectionString = "Host=172.16.0.135; Database=demoagileconnect; Username=demoagileconnect; Password=log";
            ChatClient client = new(model: "gpt-4o", apiKey: key);


            try
            {
                ChatCompletion completion = await client.CompleteChatAsync(input.prompt);

                var contentList = completion.Content;
                if (contentList == null || contentList.Count == 0)
                {
                    return StatusCode(404, new { success = false, message = "No content found in the completion response." });
                }

                var firstContent = contentList[0]?.Text;
                if (string.IsNullOrEmpty(firstContent))
                {
                    return StatusCode(404, new { success = false, message = "Text content is empty or null." });
                }

                var matches = Regex.Matches(firstContent, @"```([\s\S]*?)```");
                if (matches.Count == 0)
                {
                    return StatusCode(400, new { success = false, message = "No SQL content found within triple backticks." });
                }

                List<string> extractedContents = new List<string>();
                foreach (System.Text.RegularExpressions.Match match in matches)
                {
                    string extractedContent = match.Groups[1].Value.Trim();
                    string cleanedContent = Regex.Replace(extractedContent, @"\bsql\b", string.Empty, RegexOptions.IgnoreCase);
                    bool success = await ExecuteSQLAsync(connectionString, cleanedContent);
                    if (!success)
                    {
                        return StatusCode(500, new { success = false, message = "SQL execution failed for: " + cleanedContent });
                    }

                    extractedContents.Add(cleanedContent); 
                }

                return Ok(new { success = true, extractedContents });
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine(ex.Message);
                return StatusCode(500, new { success = false, message = $"An error occurred: {ex.Message}" });
            }
        }

        [HttpPost("api/v1/AIChatImportToForm")]
        public async Task<IActionResult> AIChatImportToForm([FromBody] ARMAI input)
        {
            // Configuration values
            string Project = _configuration["AppConfig:Project"];
            string Submitapiurl = _configuration["AppConfig:submitapiurl"];
            string username = _configuration["AppConfig:username"];
            string formname = _configuration["AppConfig:TstructName"];

            if (string.IsNullOrWhiteSpace(input?.prompt))
            {
                return BadRequest(new { success = false, message = "Input JSON string is empty or null." });
            }

            // Parse JSON input
            JArray promptArray;
            try
            {
                promptArray = JArray.Parse(input.prompt);
            }
            catch (JsonReaderException)
            {
                return BadRequest(new { success = false, message = "Invalid JSON format." });
            }

            string key = "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";

            ChatClient client = new(model: "gpt-4o", apiKey: key);
            ChatCompletion completion = await client.CompleteChatAsync(input.prompt);

            var contentList = completion.Content;
            if (contentList == null || contentList.Count == 0)
            {
                return BadRequest(new { success = false, message = "Failed to get a valid response from the AI chat." });
            }

            // Loop through each item in promptArray and call Submitapiurl
            var api = new API();
            var results = new List<object>();
            foreach (var item in promptArray)
            {
                JObject inputObject = JObject.FromObject(item);

                var formattedOutputs = new JObject
                {
                    ["data"] = new JObject
                    {
                        ["mode"] = "new",
                        ["keyvalue"] = "",
                        ["recordid"] = "0",
                        ["dc1"] = new JObject
                        {
                            ["row1"] = inputObject
                        }
                    }
                };

                var finalOutput = new JObject
                {
                    ["trace"] = "false",
                    ["keyfield"] = "",
                    ["dataarray"] = new JObject
                    {
                        ["data"] = formattedOutputs["data"]
                    }
                };

                AddAxpertNodesToJson(ref finalOutput, Project, formname, username);
                string apiInput = JsonConvert.SerializeObject(new { submitdata = finalOutput });

                try
                {
                    ARMResult armResult = await api.POSTData(Submitapiurl, apiInput, "application/json");

                    // Parse the response
                    JObject resultJsonObj = JObject.Parse(armResult.result["message"].ToString());
                    string status = resultJsonObj["status"].ToString();
                    string data = resultJsonObj["result"].ToString();

                    // Collect the result
                    results.Add(new
                    {
                        status = status,
                        result = data
                    });
                }
                catch (Exception ex)
                {
                    results.Add(new
                    {
                        status = "error",
                        result = $"Error processing item: {ex.Message}"
                    });
                }
            }

            // Return all results as a single response
            return Ok(new { results });
        }



        private void AddAxpertNodesToJson(ref JObject finalOutput, string Project, string formname, string username)
        {
            _common.AddJsonKey(ref finalOutput, "username", username);
            _common.UpdateOrAddJsonKey(ref finalOutput, "project", Project);
            _common.UpdateOrAddJsonKey(ref finalOutput, "name", formname);

            AxpertRestAPIToken axpertToken = new AxpertRestAPIToken(finalOutput["username"].ToString());
            _common.RemoveJsonKey(ref finalOutput, "username");
            _common.UpdateOrAddJsonKey(ref finalOutput, "token", axpertToken.token);
            _common.UpdateOrAddJsonKey(ref finalOutput, "seed", axpertToken.seed);
            _common.UpdateOrAddJsonKey(ref finalOutput, "userauthkey", axpertToken.userAuthKey);
        }


        [HttpPost("api/v1/GetFormDetails")]
        public async Task<IActionResult> GetFormDetailsfROMdb([FromBody] string formname)
        {
            if (string.IsNullOrEmpty(formname))
                return BadRequest(new { success = false, message = "Table name is required." });

            string connectionString = "Host=172.16.0.135; Database=demoagileconnect; Username=demoagileconnect; Password=log";
            string table = "axpflds";
            string Project = _configuration["AppConfig:Project"];
            var dataTable = await GetFormDetails(Project, formname);

            if (dataTable.Rows.Count == 0)
            {
                return NotFound(new { success = false, message = "No data found for the specified table." });
            }

            var columnStrings = new List<string>();
            foreach (DataRow row in dataTable.Rows)
            {
                string columnString = $"({row["caption"]}, {row["datatype"]})";
                columnStrings.Add(columnString);
            }

            string columnsString = string.Join(", ", columnStrings);

            Console.WriteLine("ColumnsData: " + columnsString);
            return Ok(new { success = true, columnsdata = columnsString });
        }


        private async Task<DataTable> GetFormDetails(string Project, string formname)
        {
            try
            {
                var context = new ARMCommon.Helpers.DataContext(_configuration);
                var redis = new RedisHelper(null);
                var utils = new Utils(_configuration, context, redis);
                Dictionary<string, string> config = await utils.GetDBConfigurations(Project);
                string connectionString = config["ConnectionString"];
                string dbType = config["DBType"];
                string sql = Constants_SQL.GetFormDetails;
                string[] paramNames = { "@formname" };
                DbType[] paramTypes = { DbType.String };
                object[] paramValues = { formname };

                IDbHelper dbHelper = DBHelper.CreateDbHelper(sql, dbType, connectionString, paramNames, paramTypes, paramValues);
                DataTable dataTable = await dbHelper.ExecuteQueryAsync(sql, connectionString, paramNames, paramTypes, paramValues);
                return dataTable;

            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in GetDocumentJson: {ex.Message}");
                throw;
            }

        }

        

        [HttpPost("api/v1/GetColumnsAndGenerateInsertQuery")]
        public async Task<IActionResult> GetColumnsAndGenerateInsertQuery([FromBody] string tableName)
        {
            if (string.IsNullOrEmpty(tableName))
                return BadRequest(new { success = false, message = "Table name is required." });

            string connectionString = "Host=172.16.0.135; Database=demoagileconnect; Username=demoagileconnect; Password=log";

            try
            {
                if (!await TableExists(connectionString, tableName))
                {
                    return NotFound(new { success = false, message = $"Table '{tableName}' does not exist." });
                }

                var columnNamesAndTypes = await GetColumnNamesAndTypes(connectionString, tableName);

                var result = columnNamesAndTypes.Select(c => new { ColumnName = c.ColumnName, ColumnType = c.ColumnType }).ToArray();

                return Ok(new { success = true, columnNamesAndTypes = result });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = "An error occurred.", error = ex.Message });
            }
        }

        private async Task<bool> TableExists(string connectionString, string tableName)
        {
            string query = "SELECT EXISTS (SELECT 1 FROM information_schema.tables WHERE table_schema = 'public' AND table_name = @TableName)";

            using (var connection = new NpgsqlConnection(connectionString))
            {
                await connection.OpenAsync();
                var result = await connection.ExecuteScalarAsync<bool>(query, new { TableName = tableName });
                return result;
            }
        }

        private async Task<List<(string ColumnName, string ColumnType)>> GetColumnNamesAndTypes(string connectionString, string tableName)
        {
            var columns = await ExecuteSQLAsyncForColumns(connectionString, tableName);
            return columns;
        }

        private async Task<List<(string ColumnName, string ColumnType)>> ExecuteSQLAsyncForColumns(string connectionString, string tableName)
        {
            try
            {
                // Query to get column names and data types
                var columnQuery = @"
            SELECT column_name, data_type 
            FROM information_schema.columns 
            WHERE table_name = @tableName 
            ORDER BY ordinal_position;";

                using var connection = new NpgsqlConnection(connectionString);
                await connection.OpenAsync();

                // Fetch columns
                using var columnCommand = new NpgsqlCommand(columnQuery, connection);
                columnCommand.Parameters.AddWithValue("tableName", tableName);
                using var columnReader = await columnCommand.ExecuteReaderAsync();

                var columns = new List<(string ColumnName, string ColumnType)>();
                while (await columnReader.ReadAsync())
                {
                    var columnName = columnReader.GetString(0);
                    var columnType = columnReader.GetString(1);
                    columns.Add((columnName, columnType));
                }
                columnReader.Close();

                return columns;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
                throw;
            }
        }


        static async Task<bool> ExecuteSQLAsync(string connectionString, string sql)
        {
            DataTable resultTable = new DataTable(); 
            try
            {
                string dbType = "postgre";
                IDbHelper dbHelper = DBHelper.CreateDbHelper(sql, dbType, connectionString, new string[] { }, new DbType[] { }, new object[] { });
                resultTable = await dbHelper.ExecuteQueryAsync(sql, connectionString, new string[] { }, new DbType[] { }, new object[] { });
                foreach (DataRow row in resultTable.Rows)
                {
                    if (row.ItemArray.OfType<object>().Any(item => item.ToString().Contains("Duplicate key violation.")))
                    {
                        return false;
                    }
                }
                return true; 
            }
            catch (PostgresException ex) when (ex.SqlState == "23505") 
            {
                resultTable.Columns.Add("Message");
                resultTable.Rows.Add($"Duplicate key value violates unique constraint: {ex.Message}");
                return false;
            }
            catch (Exception ex)
            {
                resultTable.Columns.Add("Message");
                resultTable.Rows.Add($"Error: {ex.Message}");
                return false; 
            }
        }

       

        [HttpGet("GetHtml")]
        public IActionResult GetHtml()
        {
            string htmlContent = @"
    <!DOCTYPE html>
    <html lang=""en"">
    <head>
    <meta charset=""UTF-8"">
    <meta name=""viewport"" content=""width=device-width, initial-scale=1.0"">
    <title>AI Chat Interface</title>
    <style>
        body {
            font-family: 'Segoe UI', Tahoma, Geneva, Verdana, sans-serif;
            background-color: #f4f6f9;
            display: flex;
            justify-content: center;
            align-items: center;
            height: 100vh;
            margin: 0;
        }
        .chat-container {
            background-color: white;
            border-radius: 12px;
            box-shadow: 0 8px 16px rgba(0, 0, 0, 0.1);
            width: 750px; /* Increased width */
            display: flex;
            flex-direction: column;
            overflow: hidden;
            max-height: 600px;
        }
        .chat-header {
            background-color: #4a90e2;
            color: white;
            padding: 20px;
            text-align: center;
            font-size: 20px;
            font-weight: bold;
            display: flex;
            justify-content: center;
            align-items: center;
        }
        .chat-header img {
            height: 40px;
            margin-right: 10px;
        }
        .chat-messages {
            flex-grow: 1;
            overflow-y: auto;
            padding: 20px;
            background-color: #fafafa;
        }
        .message {
            margin-bottom: 20px;
            display: flex;
            flex-direction: column;
            align-items: flex-start;
        }
        .ai-message {
            background-color: #e6f2ff;
            color: #2c3e50;
            padding: 12px;
            border-radius: 15px;
            max-width: 80%;
            margin-right: auto;
            box-shadow: 0 4px 10px rgba(0, 0, 0, 0.1);
        }
        .user-message {
            background-color: #dcf8c6;
            color: #2c3e50;
            padding: 12px;
            border-radius: 15px;
            max-width: 80%;
            margin-left: auto;
            box-shadow: 0 4px 10px rgba(0, 0, 0, 0.1);
        }
        .response-buttons {
            margin-top: 15px;
            display: flex;
            gap: 10px;
            align-items: center;
        }
        .dropdown-select, .normal-btn {
            padding: 10px 15px;
            border-radius: 5px;
            font-size: 14px;
            border: 1px solid #4a90e2;
        }
        .dropdown-select {
            flex-grow: 1;
            border-radius: 5px;
        }
        .normal-btn {
            background-color: #4a90e2;
            color: white;
            border: none;
            cursor: pointer;
        }
        .submit-btn {
            background-color: #4a90e2;
            color: white;
            border: none;
            cursor: pointer;
        }
        .chat-input {
            display: flex;
            padding: 15px;
            background-color: #fff;
            border-top: 1px solid #e6e6e6;
        }
        .chat-input input {
            flex-grow: 1;
            padding: 12px;
            border: 1px solid #ddd;
            border-radius: 5px;
            font-size: 14px;
        }
        .chat-input button {
            padding: 12px 18px;
            background-color: #4a90e2;
            color: white;
            border: none;
            border-radius: 5px;
            cursor: pointer;
            font-size: 14px;
        }
        .loading {
            text-align: center;
            color: #666;
            margin: 10px 0;
        }
        #ai-display {
            margin-top: 20px;
            padding: 15px;
            background-color: #f9f9f9;
            border: 1px solid #ccc;
            border-radius: 10px;
        }
.text-box {
    display: flex;
    justify-content: space-between;
    align-items: center;
    margin-top: 10px;
    background-color: #f4f8fc;
    padding: 10px;
    border-radius: 8px;
    box-shadow: 0 4px 6px rgba(0, 0, 0, 0.1);
}

.input-field {
    width: 70%;
    padding: 8px 12px;
    border-radius: 5px;
    border: 1px solid #ccc;
    font-size: 14px;
    outline: none;
    transition: border-color 0.3s ease;
}

.input-field:focus {
    border-color: #007bff;
}

.submit-btn {
    padding: 8px 16px;
    background-color: #007bff;
    color: white;
    border: none;
    border-radius: 5px;
    cursor: pointer;
    font-size: 14px;
    transition: background-color 0.3s ease;
}

.submit-btn:hover {
    background-color: #0056b3;
}

.submit-btn:focus {
    outline: none;
}

    </style>
</head>
<body>
    <div class=""chat-container"">
        <div class=""chat-header"">
          AI Assistant
        </div>

        <div class=""chat-messages"" id=""chatMessages"">
            <div class=""message"">
                <div class=""ai-message"">
                    Hello! I'm an AI assistant. How can I help you today?
                </div>
            </div>
        </div>
        <div class=""chat-input"">
            <input type=""text"" id=""messageInput"" placeholder=""Type your message..."">
            <button onclick=""sendMessage()"">Send</button>
        </div>
       </div>

   <script>
  let extractedContents = '';

  async function sendMessage() {
    const input = document.getElementById('messageInput');
    const chatMessages = document.getElementById('chatMessages');
    const prompt = input.value.trim();
    
    if (prompt === '') return;

    // Add user message
    const userMessageDiv = document.createElement('div');
    userMessageDiv.className = 'message';
    userMessageDiv.innerHTML = `
        <div class=""user-message"">
            ${prompt}
        </div>
    `;
    chatMessages.appendChild(userMessageDiv);

    // Add loading indicator
    const loadingDiv = document.createElement('div');
    loadingDiv.className = 'loading';
    loadingDiv.textContent = 'Generating response...';
    chatMessages.appendChild(loadingDiv);

    chatMessages.scrollTop = chatMessages.scrollHeight;

    try {
        const response = await fetch('/api/v1/AIChat', {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify({ prompt })
        });

        // Remove loading indicator
        chatMessages.removeChild(loadingDiv);

        if (response.ok) {
            const data = await response.json();
            extractedContents = data.extractedContents; 
            if (data.extractedContents) {
                const aiMessageDiv = document.createElement('div');
                aiMessageDiv.className = 'message';
                aiMessageDiv.innerHTML = `
                    <div class=""ai-message"">
                        ${data.extractedContents}
                        <div class=""response-buttons"">
                           <button class=""normal-btn"" onclick=""handleContinueButton(this)"">Import Existing</button>
                            <button class=""normal-btn"" onclick=""handleImportButtonClick(this)"">Import New</button>
                            <button class=""normal-btn"" onclick=""handleImportButtonClickForm(this)"">Import Data To Form</button>
                        </div>
                    </div>
                `;
                chatMessages.appendChild(aiMessageDiv);
            } else {
                const aiMessageDiv = document.createElement('div');
                aiMessageDiv.className = 'message';
                aiMessageDiv.innerHTML = `
                    <div class=""ai-message"">
                        No meaningful response received.
                    </div>
                `;
                chatMessages.appendChild(aiMessageDiv);
            }
        } else {
            const aiMessageDiv = document.createElement('div');
            aiMessageDiv.className = 'message';
            aiMessageDiv.innerHTML = `
                <div class=""ai-message"">
                    Error: ${response.statusText}
                </div>
            `;
            chatMessages.appendChild(aiMessageDiv);
        }
    } catch (error) {
        if (loadingDiv.parentNode) {
            chatMessages.removeChild(loadingDiv);
        }
        const aiMessageDiv = document.createElement('div');
        aiMessageDiv.className = 'message';
        aiMessageDiv.innerHTML = `
            <div class=""ai-message"">
                Error: ${error.message}
            </div>
        `;
        chatMessages.appendChild(aiMessageDiv);
    }

    input.value = '';
    chatMessages.scrollTop = chatMessages.scrollHeight;
}

function handleImportButtonClickForm(button) {
    const parentAiMessage = button.closest('.ai-message'); 

    // Remove any existing text box and submit button to ensure new values are entered
    const existingTextBox = parentAiMessage.querySelector('.text-box');
    if (existingTextBox) {
        existingTextBox.remove();
    }

    // Add the text box and submit button only if they don't already exist
    if (!parentAiMessage.querySelector('.text-box')) {
        const textBoxDiv = document.createElement('div');
        textBoxDiv.className = 'text-box';
        textBoxDiv.innerHTML = `
            <input type=""text"" id=""textBoxInputForm"" placeholder=""Enter Existing Form Name here"" class=""input-field""/>
            <button class=""submit-btn"" onclick=""ImportFormButton(this)"">Submit</button>
        `;
        parentAiMessage.appendChild(textBoxDiv);
    }
}

function handleContinueButton(button) {
    const parentAiMessage = button.closest('.ai-message'); 

    // Remove any existing text box and submit button to ensure new values are entered
    const existingTextBox = parentAiMessage.querySelector('.text-box');
    if (existingTextBox) {
        existingTextBox.remove();
    }

    // Add the text box and submit button only if they don't already exist
    if (!parentAiMessage.querySelector('.text-box')) {
        const textBoxDiv = document.createElement('div');
        textBoxDiv.className = 'text-box';
        textBoxDiv.innerHTML = `
            <input type=""text"" id=""textBoxInput"" placeholder=""Enter Import Existing Table Name here"" class=""input-field""/>
            <button class=""submit-btn"" onclick=""handleNormalButton(this)"">Submit</button>
        `;
        parentAiMessage.appendChild(textBoxDiv);
    }
}

function handleImportButtonClick(button) {
    const parentAiMessage = button.closest('.ai-message');

    // Remove any existing text box and submit button to ensure new values are entered
    const existingTextBox = parentAiMessage.querySelector('.text-box');
    if (existingTextBox) {
        existingTextBox.remove();
    }

    // Add a new text box and submit button dynamically when ""Import Create"" is clicked
    const textBoxDiv = document.createElement('div');
    textBoxDiv.className = 'text-box';
    textBoxDiv.innerHTML = `
        <input type=""text"" id=""textBoxImportInput"" placeholder=""Enter New Table here..."" class=""input-field""/>
        <button class=""submit-btn"" onclick=""handleImportCreateButton(this)"">Submit</button>
    `;
    parentAiMessage.appendChild(textBoxDiv);
}

// Import to form
// Import to form
async function ImportFormButton(button) {
    const parentAiMessage = button.closest('.ai-message');
    const inputField = parentAiMessage.querySelector('#textBoxInputForm');
    const formname = inputField.value.trim(); // Trim whitespace from formname

    if (!formname) {
        console.error(""Form name is empty. Please provide a valid form name."");
        addToAIDisplay(""Error: Form name cannot be empty."");
        return;
    }

    console.log(""Form name:"", formname);

    // Remove the text box div after submit
    const textBoxDiv = parentAiMessage.querySelector('.text-box');
    if (textBoxDiv) {
        textBoxDiv.remove();
    }

    try {
        // Fetch columns data from the GetFormDetails API
        const aiChatFormResponse = await fetch('http://localhost:5000/api/v1/GetFormDetails', {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify(formname) // Pass formname as an object
        });

        if (!aiChatFormResponse.ok) {
            const errorResponse = await aiChatFormResponse.json();
            console.error('Error from GetFormDetails:', errorResponse.message);
            addToAIDisplay(`Error fetching form details: ${errorResponse.message}`);
            return;
        }

        const data = await aiChatFormResponse.json();
        const columns = data.columnsdata; // Assuming you need this for later use
        console.log('Columns data:', columns);

        let modifiedValue = String(extractedContents).replace('json', '').trim();
        modifiedValue = modifiedValue.replace(/\\n/g, '').replace(/\s+/g, '');

        try {
            const parsedJSON = JSON.parse(modifiedValue); // Validate JSON
            modifiedValue = JSON.stringify(parsedJSON);
        } catch (error) {
            console.error(""Failed to parse JSON:"", error);
            addToAIDisplay(""Error: Failed to parse JSON content."");
            return;
        }

        const requestBody = { prompt: modifiedValue, formname };
        console.log(""Request body for AIChatImportToForm:"", requestBody);

        // Post to AIChatImportToForm API
        const aiChatResponse = await fetch('http://localhost:5000/api/v1/AIChatImportToForm', {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify(requestBody)
        });

        if (aiChatResponse.ok) {
            const aiData = await aiChatResponse.json();
            console.log('AIChatImportToForm response:', aiData);
            addToAIDisplay(""Data imported to the Form successfully!"");
        } else {
            let errorDetails;
            try {
                errorDetails = await aiChatResponse.json();
            } catch (parseError) {
                errorDetails = { message: await aiChatResponse.text() || ""Unknown error"" };
            }
            console.error('AIChatImportToForm API Error Details:', errorDetails);
            addToAIDisplay(`Error importing data: ${errorDetails.message || ""No additional details available.""}`);
        }
    } catch (error) {
        console.error('Fetch error:', error);
        addToAIDisplay(""Error: Failed to communicate with the server."");
    }
}


async function handleImportCreateButton(button) {
    const parentAiMessage = button.closest('.ai-message');
    const inputField = parentAiMessage.querySelector('#textBoxImportInput');
    const input = inputField.value;
    console.log(input);  // Example: Logging the input value

    // Remove the text box div after submit
    const textBoxDiv = parentAiMessage.querySelector('.text-box');
    if (textBoxDiv) {
        textBoxDiv.remove();
    }

    let valueExtracted = JSON.stringify(extractedContents);
    let modifiedValue = valueExtracted.slice(1, -1);
    modifiedValue = modifiedValue.slice(1, -1);

    const finalString = `${modifiedValue}. create table ${input}. statement and insert statements separately for this data to import into my postgres database.`;

    console.log('Final JSON:', finalString);

    const requestBody = { prompt: finalString };

    const aiChatResponse = await fetch('http://localhost:5000/api/v1/AIChatImport', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify(requestBody)
    });

    if (aiChatResponse.ok) {
        const aiData = await aiChatResponse.json();
        addToAIDisplay(""Data imported to the New table successfully!"");
    } else {
        let errorDetails;
        try {
            errorDetails = await aiChatResponse.json();
        } catch (parseError) {
            errorDetails = { message: await aiChatResponse.text() || ""Unknown error"" };
        }
        console.error('AIChat API Error Details:', errorDetails);
        addToAIDisplay(`Error importing data: ${errorDetails.message || ""No additional details available.""}`);
    }
}



async function handleNormalButton(button) {
    const parentAiMessage = button.closest('.ai-message');

    // Do something with the input value here (e.g., process or send the input data)
    const inputField = parentAiMessage.querySelector('#textBoxInput');
    const input = inputField.value;
    console.log(input);  // Example: Logging the input value

    // Remove the text box div after submit
    const textBoxDiv = parentAiMessage.querySelector('.text-box');
    if (textBoxDiv) {
        textBoxDiv.remove();
    }

    let valueExtracted = JSON.stringify(extractedContents);
    let modifiedValue = valueExtracted.slice(1, -1);
    modifiedValue = modifiedValue.slice(1, -1);

    try {
        // Fetch columns and generate insert query
        const columnResponse = await fetch('/api/v1/GetColumnsAndGenerateInsertQuery', {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json'
            },
            body: JSON.stringify(input)
        });

        if (!columnResponse.ok) {
            const errorDetails = await columnResponse.json();
            console.error('Server error details:', errorDetails);
            throw new Error(`Failed to fetch columns: ${errorDetails.message || columnResponse.statusText}`);
        }

        const columnData = await columnResponse.json();
        console.log('Full response data:', columnData);

        if (columnData.success) {
            const columns = columnData.columnNamesAndTypes;

            if (Array.isArray(columns) && columns.length > 0) {
                // Combine column names and types
                const columnDataString = columns.map(column => {
                    return `${column.columnName} (${column.columnType})`;
                }).join("", "");

                console.log('All columns with types:', columnDataString);

                const finalString = `${modifiedValue}. My table is ${input}. (${columnDataString}). Create insert statements to insert data into my PostgreSQL table based on the above JSON.`;

                console.log('Final JSON:', finalString);

                const requestBody = {
                    prompt: finalString
                };

                const aiChatResponse = await fetch('http://localhost:5000/api/v1/AIChatImport', {
                    method: 'POST',
                    headers: {
                        'Content-Type': 'application/json',
                    },
                    body: JSON.stringify(requestBody)
                });

                if (aiChatResponse.ok) {
                    const aiData = await aiChatResponse.json();
                    addToAIDisplay(""Data imported to the existing table successfully!"");
                } else {
                    let errorDetails;
                    try {
                        // Try parsing error details from the response body
                        errorDetails = await aiChatResponse.json();
                    } catch (parseError) {
                        // If parsing fails, fallback to plain text or default error
                        errorDetails = { message: await aiChatResponse.text() || ""Unknown error"" };
                    }

                    console.error('AIChat API Error Details:', errorDetails);
                    throw new Error(
                        `AIChat API request failed:
                        Status: ${aiChatResponse.status} ${aiChatResponse.statusText}
                        Error Message: ${errorDetails.message || ""No additional details available.""}`
                    );
                }
            } else {
                console.error('Error: Columns data is empty or not in the expected format.');
                addToAIDisplay(""Error: Unable to retrieve valid column data. Please check the input and try again."");
            }
        } else {
            console.error('Server response error:', columnData.message);
            addToAIDisplay(`Error: ${columnData.message || ""An unknown error occurred while processing the column data.""}`);
        }
    } catch (error) {
        console.error('Unhandled Error:', error);
        addToAIDisplay(`Error: ${error.message || ""An unexpected error occurred. Please try again later.""}`);
    }
}
function addToAIDisplay(message) {
    const chatMessages = document.getElementById('chatMessages');
    const aiMessageDiv = document.createElement('div');
    aiMessageDiv.className = 'message';
    aiMessageDiv.innerHTML = `
        <div class=""ai-message"">
            ${message}
        </div>
    `;
    chatMessages.appendChild(aiMessageDiv);
}
</script>

</body></html>";
            return Content(htmlContent, "text/html", Encoding.UTF8);
        }




    }
}



