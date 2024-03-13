using System.Net.Http;
using System.Threading.Tasks;
using SalesBotApi.Models;

public class EmailService
{
    private readonly IHttpClientFactory clientFactory;
    private readonly QueueService<EmailRequest> queueService;

    public EmailService(
        IHttpClientFactory _clientFactory,
        QueueService<EmailRequest> _queueService
    )
    {
        clientFactory = _clientFactory;
        queueService = _queueService;
    }

    public async Task SendEmail(string _sender_email, string _sender_name, string _recipient_email, string _subject, string _body)
    {
        EmailRequest emailReq = new EmailRequest
        {
            sender_email = _sender_email,
            sender_name = _sender_name,
            recipient_email = _recipient_email,
            subject = _subject,
            body = _body
        };

        await queueService.EnqueueMessageAsync(emailReq);
    }

    public async Task SendLeadGeneratedEmail(
        string recipient_email, 
        AssistantResponse assistantResponse,
        string convo_id
    ) {
            await SendEmail(
                "hello@keli.ai", 
                "Keli.AI",
                recipient_email, 
                "New website lead from Keli.AI", 
                $@"
Hello!

Good news -- you have a new website lead from Keli.AI. Here is their information:

First Name: {assistantResponse.collected_data.affiant_information.first_name}
Last Name: {assistantResponse.collected_data.affiant_information.last_name}
Email:  {assistantResponse.collected_data.affiant_information.email_address}
Phone: {assistantResponse.collected_data.affiant_information.phone_number}
Conversation history: https://admin.keli.ai/messages?convo_id={convo_id}

Please reach out to them as soon as possible.

Thank you,
The Keli.AI team
"
            );
        }

    public async Task SendRegistrationEmail(string recipient_email) {
            await SendEmail(
                "hello@keli.ai", 
                "Keli.AI",
                recipient_email, 
                "Keli.AI registration received", 
                @"
Hello! 

Thanks for registering for a free trial account at Keli.AI. We will approve your account as quickly as possible (usually within 24 hours) and let you know. If you haven't heard from us within a few days, feel free to reply to this email. 

Thank you,
The Keli.AI team
"
            );
        }

    public async Task SendRegistrationApprovalEmail(string recipient_email) {
            await SendEmail(
                "hello@keli.ai", 
                "Keli.AI",
                recipient_email, 
                "Your Keli.AI registration was approved", 
                @"
Hello! 

Good news -- your Keli.AI registration was approved, and your account is now active. Please log in here: https://admin.keli.ai

See our Getting Started guide at https://docs.keli.ai

If you have any questions, feel free to reply to this email.

Thank you,
Keli.AIt team
"
            );
        }

        public async Task SendRegistrationDeniedEmail(string recipient_email) {
            await SendEmail(
                "hello@keli.ai", 
                "Keli.AI", 
                recipient_email, 
                "Your Keli.AI registration was declined", 
                @"
Hello! 

Unfortunately, your Keli.AI registration was declined. Be sure to use a company email address when registering for an account. We don't accept personal emails, e.g. Gmail, Yahoo, Hotmail, etc. See here for more information: https://docs.keli.ai

If you have any questions, feel free to reply to this email.

Thank you,
The Keli.AI team
"
            );
        }

        public async Task SendNewRegistrationAdminEmail() {
            await SendEmail(
                "hello@keli.ai", 
                "Keli.AI", 
                "sfncook@gmail.com,kelidotai@gmail.com", 
                "Keli.AI: New account registration", 
                @"
Hello! 

A new account has been registered in the Keli.AI Admin Portal.  It is waiting for approval.

https://admin.keli.ai/users

Thank you,
The Keli.AI team
"
            );
        }

}
