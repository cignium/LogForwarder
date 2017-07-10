Log Forwarder Extension
================

Log Forwarder is an extension that install a continuous webjob that send log from a file to Splunk in your Azure App Service.

## Gettting Started

1. Got to "Extensions" option and click on "Add".
2. Choose "Log Forwarder" extension and accept legal terms.
3. Once installed, go to App settings and configure the followings variables:
    - LOG_FILE_PATH = Path of Log File, relative to website's root
    - START_ON_HOME = Set "1" to start LOG_FILE_PATH on Home (Optional, default 0)
    - INDEX_NAME = Index where you can write
    - SPLUNK_HOSTNAME = Splunk Host where will write your logs (e.g. api.myslunk.com:8089)
    - SPLUNK_USERNAME = Splunk User Name
    - SPLUNK_PASSWORD = Splunk Password
    - DELAY = Monitor Delay in seconds (Optional, default 5)
4. You need to enable "Always On" option in App Settings for keep alive the WebJob
5. Restart your App Service.

## Notes

- Verify if your App Service has connection with your Splunk Host.

## License

[MIT License](https://raw.githubusercontent.com/cignium/LogForwarder/master/LICENSE.txt)