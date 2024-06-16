# Dab-DacFx: Generate config entities for Data API Builder with DacFx from a dacpac or a database

Proof of concept.

## Usage


Use an existing dacpac to generate a config file for Data API Builder, including the views.
```bash
dab-dacfx -d "C:\path\to\your\database.dacpac" -c "C:\path\to\your\dab\config.json" -i true
```

### Inputs

| Name | Description | Required | Default |
| --- | --- | --- |
| -d | Path to the dacpac file | true, or -s | |
| -s | Connection string to the database | true, or -d | |
| -c | Path to the config file | true | |
| -i | Include views in the config file | false | false |
| -p | Permissions to apply to the entities | false | "anonymous:*" |
| -h | Show help | false | |

## Notes

There's no library to manipulate the Data API Builder config files, so I went with using the CLI manually.
- https://github.com/Azure/data-api-builder
- https://learn.microsoft.com/en-us/azure/data-api-builder/reference-configuration#actions


The TSqlModel from either the DacPac or the SQL connection will also contain information about foreign keys that can be used to generate the relationships in the config file.