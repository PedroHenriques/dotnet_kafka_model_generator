# Kafka Topic Model Generator
A tool that parses Kafka topic schemas and generates C# POCO models that can be used with Confluent's SDK to publish and subscribe to topics.

## Main functionalities
- Parses Kafka topic schemas. Supported formats:
  - `json`
  - `avro`
- Outputs C# POCO models tailored to work with Confluent's .Net SDK serializers
- Outputs the Kafka topic schemas, used as inputs with any necessary adjustments to work with the generated models

# Installing the tool
```sh
# Install as a global tool: Makes the tool available system-wide (in your PATH)
dotnet tool install -g KafkaModelGenerator

# Install a specific version, as a global tool
dotnet tool install -g KafkaModelGenerator --version <VERSION>

---

# Install as a local tool: Installs into .config/dotnet-tools.json
dotnet new tool-manifest   # only once per repo
dotnet tool install KafkaModelGenerator

# Install a specific version, as a local tool
dotnet tool install KafkaModelGenerator --version <VERSION>
```

# Using the tool
```sh
Usage:
  kafka-model-generator --schema-content "<schema-content>" --schema-type "<schema-type>" --output-dir "<output-directory>" [--namespace "<namespace>"] [--root-class-name "<root-class-name>"]
  kafka-model-generator --schema-file "<schema-file-path>" --schema-type "<schema-type>" --output-dir "<output-directory>" [--namespace "<namespace>"] [--root-class-name "<root-class-name>"]

Examples:
  kafka-model-generator --schema-content "{ \"title\": \"OrderCreated\", \"type\": \"object\", \"properties\": { \"id\": { \"type\": \"string\" } }, \"required\": [\"id\"] }" --schema-type "json" --output-dir "./Generated"

  kafka-model-generator --schema-file "./schemas/order-created.json" --schema-type "json" --output-dir "./Generated" --namespace "MyCompany.Kafka.Models"

Notes:
  --schema-content    Full schema content as a string
  --schema-file       Path to a file containing the JSON schema
  --schema-type       One of the supported schema types. Accepts: json
  --output-dir        Directory where generated files will be written
  --namespace         Optional. Default: Generated.Kafka.Models
  --root-class-name   Optional. Overrides the generated root class name
  --help              Show help
```

## Notes
- 1 and only 1 of `--schema-content` and `--schema-file` must be provided
- The tool will generate 2 files in the provided `--output-dir`:
  - 1 file with the C# POCOs
  - 1 file with the normalized schema, with potential necessary adjustments to make it work with Confluent's .Net SDK
- Confluent's .Net SDK expects the schema's title to match the C# class name. The tool will follow this priority when de termining the generated C# class name:
  - `--root-class-name`, if provided
  - The schema's `title` (for JSON) or `name` (for AVRO) property, if present
  - The default value: KafkaMessage