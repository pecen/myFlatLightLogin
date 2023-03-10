If a nuget configuration file is to be used, do the following:

1. Create a file in the solution root directory called nuget.config
2. Copy and Paste the following to that file:

```
<?xml version="1.0" encoding="utf-8"?>
<configuration>
  <config>
    <add key="repositoryPath" value="..\lib" />
  </config>
</configuration>
```

3. Save and close the file
