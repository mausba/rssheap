//MYSQL settings
###########################################################################################################
1. Download MySQL 5.7.17
2. Create schema (database) as 'rssheap' in MySQL Workbench
3. Import 'Schema' folder from MySQLDB folder, then import 'Data' folder same way
4. Open and execute 'rssheap_routines' procedure.
5. Add value="server=localhost;port=3306;user=;password=;database=rssheap" to connstring in  Web.config
###########################################################################################################
//STRIPE PAYMENT SERVICE
###########################################################################################################
Add your API keys in Web.config for testing

<add key="StripePublishableKey" value="pk_test_51Pc5uRB2uN1f0r7OV4NUGS2TbcHSd2bUijhtC3GTWC41mMt4jrdGJtK5N5qLNLjWegF7pzxeWHASjOeId9QlhFCb00yEZksJdi" />
<add key="StripeSecretKey" value="sk_test_51Pc5uRB2uN1f0r7OxUgLjRJom0cLpiua7yKTubJGGM4nfEbRlJEgH69yQXzSH6jv61W9hMrwjubNsUWvLxIrgE2800ft4kAM0V" />
