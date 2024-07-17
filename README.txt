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

<add key="StripePublishableKey" value="YOUR KEY" />
<add key="StripeSecretKey" value="YOUR KEY" />
