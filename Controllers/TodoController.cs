using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using TodoApi.Models;
using Microsoft.Azure.Cosmos;
using System;
using Microsoft.Extensions.Configuration;

namespace TodoApi.Controllers
{
    [Route("api/[controller]")] 
    [ApiController]
    public class TodoController : Controller
    {
        private readonly TodoContext _context;

        public TodoController(TodoContext context)
        {
            _context = context;

//            Console.WriteLine("_context.TodoItems.Count()");
//            Console.WriteLine(_context.TodoItems.Count());
            if (_context.TodoItems.Count() == 0)
            {
                _context.TodoItems.Add(new TodoItem { Name = "Item1" });
                _context.SaveChanges();
            }
        }

        // GET: api/Todo
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Message>>> GetTodoItems([FromQuery] int offset = 0, [FromQuery] int limit = 10)
        {
            Console.WriteLine("YO MAMA XXX");
            CosmosClient client = new CosmosClient(
                        "https://keli-chatbot-02.documents.azure.com:443/",
                        "r5Mvqb5nf0G9uILKYTl0XTQHSMcxerm65qwm22ePQTIhQTxqnSPk8qosd2qaNjT0zx25XhK1i6jvACDbEcDLTg==",
                        new CosmosClientOptions()
                        {
                            ApplicationRegion = Regions.EastUS2,
                        });
            Database database = client.GetDatabase("keli");
            Container container = database.GetContainer("messages_sales");

//            ItemResponse<Message> response = await container.ReadItemAsync<Message>(
//                            id: "e3c73e08-4317-4912-8397-6ba42d807034",
//                            partitionKey: new PartitionKey("08ab1b33-6c8a-45db-abfc-043f96b891b8")
//                        );
//            Console.WriteLine($"Read item id:\t{response.Resource.id}\t{response.Resource._ts}\t{response.Resource.conversation_id}\t{response.Resource.user_msg}\t{response.Resource.assistant_response}");
//            //            return await _context.TodoItems.ToListAsync();
//            return response.Resource;

            // Define a SQL query string to get the first 2 messages
//            string sqlQueryText = $"SELECT * FROM c ORDER BY c._ts DESC OFFSET {offset} LIMIT {limit}";
            string sqlQueryText = $"SELECT * FROM c WHERE IS_STRING(c.company_id) OFFSET {offset} LIMIT {limit}";
//            string sqlQueryText = $"SELECT count(*) FROM c";
            Console.WriteLine(sqlQueryText);

            QueryDefinition queryDefinition = new QueryDefinition(sqlQueryText);
            List<Message> messages = new List<Message>();

            using (FeedIterator<Message> feedIterator = container.GetItemQueryIterator<Message>(
                queryDefinition))
            {
                while (feedIterator.HasMoreResults)
                {
                    FeedResponse<Message> response = await feedIterator.ReadNextAsync();
                    messages.AddRange(response.ToList());
                }
            }

            return messages;
        }

        // GET: api/Todo/foo_001
        [HttpGet("foo_001")]
        public async Task<ActionResult<IEnumerable<MessagesManyPerConvo>>> GetTodoItems()
        {
            Console.WriteLine("YO MAMA foo_001");
            CosmosClient client = new CosmosClient(
                        "https://keli-chatbot-02.documents.azure.com:443/",
                        "r5Mvqb5nf0G9uILKYTl0XTQHSMcxerm65qwm22ePQTIhQTxqnSPk8qosd2qaNjT0zx25XhK1i6jvACDbEcDLTg==",
                        new CosmosClientOptions()
                        {
                            ApplicationRegion = Regions.EastUS2,
                        });
            Database database = client.GetDatabase("keli");
            Container container = database.GetContainer("messages_sales");

            // Define a SQL query string to get the first 2 messages
            string sqlQueryText = "SELECT count(m) as many_msgs, m.conversation_id FROM m GROUP BY m.conversation_id";
            Console.WriteLine(sqlQueryText);

            QueryDefinition queryDefinition = new QueryDefinition(sqlQueryText);
            List<MessagesManyPerConvo> messages = new List<MessagesManyPerConvo>();

            using (FeedIterator<MessagesManyPerConvo> feedIterator = container.GetItemQueryIterator<MessagesManyPerConvo>(
                queryDefinition))
            {
                while (feedIterator.HasMoreResults)
                {
                    FeedResponse<MessagesManyPerConvo> response = await feedIterator.ReadNextAsync();
                    messages.AddRange(response.ToList());
                }
            }

            return messages;
        }


        // GET: api/Todo/5
        [HttpGet("{id}")]
        public async Task<ActionResult<TodoItem>> GetTodoItem(long id)
        {
            var todoItem = await _context.TodoItems.FindAsync(id);

            if (todoItem == null)
            {
                return NotFound();
            }

            return todoItem;
        }

        // PUT: api/Todo/5
        // To protect from overposting attacks, please enable the specific properties you want to bind to, for
        // more details see https://aka.ms/RazorPagesCRUD.
        [HttpPut("{id}")]
        public async Task<IActionResult> PutTodoItem(long id, TodoItem todoItem)
        {
            if (id != todoItem.Id)
            {
                return BadRequest();
            }

            _context.Entry(todoItem).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!TodoItemExists(id))
                {
                    return NotFound();
                }
                else
                {
                    throw;
                }
            }

            return NoContent();
        }

        // POST: api/Todo
        // To protect from overposting attacks, please enable the specific properties you want to bind to, for
        // more details see https://aka.ms/RazorPagesCRUD.
        [HttpPost]
        public async Task<ActionResult<TodoItem>> PostTodoItem(TodoItem todoItem)
        {
            _context.TodoItems.Add(todoItem);
            await _context.SaveChangesAsync();

            return CreatedAtAction("GetTodoItem", new { id = todoItem.Id }, todoItem);
        }

        // DELETE: api/Todo/5
        [HttpDelete("{id}")]
        public async Task<ActionResult<TodoItem>> DeleteTodoItem(long id)
        {
            var todoItem = await _context.TodoItems.FindAsync(id);
            if (todoItem == null)
            {
                return NotFound();
            }

            _context.TodoItems.Remove(todoItem);
            await _context.SaveChangesAsync();

            return todoItem;
        }

        private bool TodoItemExists(long id)
        {
            return _context.TodoItems.Any(e => e.Id == id);
        }
    }
}
