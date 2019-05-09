# GenericParser

GenericParser is a .NET implementation of a parser for delimited and fixed width format files.

## Get Started

Install the [latest Nuget package](https://www.nuget.org/packages/GenericParsing).

Instantiate an instance of the parser:

```csharp
using (GenericParser parser = new GenericParser(filePath))
{
    while (parser.Read())
    {
      string id = parser["ID"];
      string name = parser["Name"];
      string status = parser["Status"];

      // Your code here ...
    }
}
```

See below for more examples.

## GenericParser Features

* Efficient
* Excellent unit test coverage
* Supports delimited and fixed-width formats
  * For delimited, any custom delimited of a single character is supported
* Supports comment rows (single character marker)
* Supports escape characters (single character only)
* Supports a custom text qualifier to allow column/row delimiters to be ignored (e.g., multi-line data)
* Supports escaped text qualifiers by doubling them up
* Supports ignoring/including rows that contain no characters
* Supports a header row
* Supports the ability to dynamically add more columns to match the data
* Supports the ability to enforce the number of columns to a specific number
* Supports the ability to enforce the number of columns based on the first row
* Supports trimming the strings of a column
* Supports stripping off control characters
* Supports reusing the same instance of the parser for different data sources
* Supports TextReader and String (the file location) as data sources
* Supports limiting the maximum number of rows to read
* Supports customizing the size of the internal buffer
* Supports skipping rows at the beginning of the data after the header row
* Supports XML configuration to configure the parser
* Supports access to data via column name (when a header row is supplied)
* Supports Unicode encoding

## GenericParserAdapter Features (Extends GenericParser)

* Supports skipping rows at the end of the data
* Supports adding a line number to each row
* Supports the following outputs - XML, DataTable, and DataSet

## GenericParser - Examples

* Parsing file from disk

```csharp
using (GenericParser parser = new GenericParser(filePath))
{
    while (parser.Read())
    {
      string id = parser["ID"];

      // Your code here ...
    }
}
```

* Parsing file from stream

```csharp
using (GenericParser parser = new GenericParser(stream))
{
    // ...
}
```

## GenericParserAdapter - Examples

* Parsing file into a DataSet

```csharp
using (GenericParserAdapter parser = new GenericParserAdapter(filePath))
{
    var dsResult = parser.GetDataSet();
}
```