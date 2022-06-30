# FNB.InContact.Parser

A parser for FNB banking InContact emails or SMSs.

[![Deploy FunctionApp](https://github.com/francoishill/FNB.InContact.Parser/actions/workflows/deploy-function-app.yml/badge.svg)](https://github.com/francoishill/FNB.InContact.Parser/actions/workflows/deploy-function-app.yml)

## Getting started

* Fork this repo
* Configure your own github action workflows (see the `./github/workflows/deploy-function-app.yml` file)
  * Ensure you specify your own value for `AZURE_FUNCTIONAPP_NAME`
* Deploy your function app and obtain its unique URL for the `ReceiveAndStoreSendGridEmail` function
  * Be sure to add Application Settings (environment variables) in your function app's "Configuration":
      * `ReceivedSendGridEmailsQueueName`: The queue name it must send/receive messages to/from
      * `ServiceBus`: Should be a Service Bus connection string to the service bus with an existing queue (with the same name used in `ReceivedSendGridEmailsQueueName` above)
* See the SendGrid documentation on how to set up [SendGrid's Inbound Parse](https://docs.sendgrid.com/for-developers/parsing-email/setting-up-the-inbound-parse-webhook)
  * Use your function app's unique URL as the "Destination URL" of the SendGrid "Inbound Parse" config 

## Credits

* Thanks to [EagerELK/fnb-incontact-parser](https://github.com/EagerELK/fnb-incontact-parser/blob/master/provisioning/50-fnb-grok-filter.conf.j2) for the useful regex patterns.
* [Handlebars-Net/Handlebars.Net](https://github.com/Handlebars-Net/Handlebars.Net)