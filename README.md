# FNB.InContact.Parser

A parser for FNB banking incontact emails or SMSs

## Getting started

* Fork this repo
* Configure your own github action workflows (see the `./github/workflows/deploy-function-app.yml` file)
  * Ensure you specify your own value for `AZURE_FUNCTIONAPP_NAME`
* Deploy your function app and obtain its unique URL
* See the SendGrid documentation on how to set up [SendGrid's Inbound Parse](https://docs.sendgrid.com/for-developers/parsing-email/setting-up-the-inbound-parse-webhook)
  * Use your function app's unique URL as the "Destination URL" of the SendGrid "Inbound Parse" config 

## Credits

Thanks to [EagerELK/fnb-incontact-parser](https://github.com/EagerELK/fnb-incontact-parser/blob/master/provisioning/50-fnb-grok-filter.conf.j2) for the useful regex patterns.