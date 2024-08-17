This application does one job only: It parses all types of Ability Mail Server logs, in bulk, extracts the data, formats the data, then inserts records concurrently, into SQL Server.
It is a console application with several command line switches that will run in Scheduler at whatever times is necessary.
It is highly optimized for throughput and was tested with directories of ~ 9000 files, each file no more than 0.5MB in size. 
Log files are hashed and the hash is stored in the db along with all the logging information present in the logs. 
After succesful ETL of the logs, they can be optionally, deleted, or archived (or do nothing).  To increase throughput, you can use the -after switch to archive or delete files AFTER all files have been processed.

The point of this app is to allow querying of logs, primarily for security information (and future action). Because the hash is stored, any changes to a log file are immediately discoverable.
The parser also puts IP addresses in long hand format for easy sorting via database queeries.

I plan on making a newer version for other mail servers as well as an interface (probably Blazor) to do some graphical reporting. I also plan on creating another app that can tap into a firewall (probably a cloud firewall) and create blocking rules where DoS or hacking attempts are made so that the mail server isn't overwhelmed.

Any ideas for improvement are welcome! (note: there will be a tutorial for this on GeekusMaximus as soon as I have a femptosecond to write one)
