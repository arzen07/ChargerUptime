# Charger Uptime Calculator

This program calculates the uptime percentage for electric vehicle charging stations based on availability reports.

## Requirements

- .NET 9.0 SDK
- Linux (amd64 architecture)

## Installation

1. Clone the repository
2. Ensure you have .NET 9.0 SDK installed:
   ```bash
   dotnet --version
   ```
   If not installed, follow [Microsoft's instructions](https://dotnet.microsoft.com/download/dotnet/9.0) for Linux amd64.

## Running the Program

1. Build the project:
   ```bash
   dotnet build
   ```

2. Run the program with your input file:
   ```bash
   dotnet run -- path/to/your/input_file.txt
   ```

## Running Tests

Execute all tests:
```bash
dotnet test
```

## Test Coverage

The test suite includes comprehensive coverage of both the file processing and uptime calculation components:

### File Processing Tests
1. **Valid Input Tests**
   - Basic valid input with multiple stations and reports
   - Non-contiguous time ranges
   - Adjacent time periods (one ends when another starts)

2. **Integer Range Tests**
   - Station ID overflow (> uint32 max)
   - Charger ID overflow (> uint32 max)
   - Time value overflow (> uint64 max)

3. **Data Format Tests**
   - Invalid boolean values
   - Invalid file format
   - Missing sections

4. **Duplicate ID Tests**
   - Duplicate station IDs
   - Duplicate charger IDs in same station
   - Duplicate charger IDs across stations

5. **Time Range Tests**
   - End time before start time
   - Overlapping time periods
   - Adjacent time periods
   - Non-contiguous time periods

6. **Missing Data Tests**
   - No stations
   - No reports
   - Missing reports for some chargers
   - Reports for non-existent chargers

### Uptime Calculation Tests
1. **Basic Calculations**
   - Single charger, single period
   - Multiple chargers, multiple periods
   - Mixed up/down status

2. **Edge Cases**
   - Zero uptime
   - 100% uptime
   - Partial period uptime

3. **Time Range Tests**
   - Non-contiguous periods
   - Adjacent periods
   - Different period lengths

## Input File Format

The input file must contain two sections:

1. `[Stations]` - List of charging stations and their chargers
2. `[Charger Availability Reports]` - Availability reports for each charger

Example:
```
[Stations]
1 1 2
2 3 4

[Charger Availability Reports]
1 0 100 true
2 50 150 false
3 0 100 true
4 0 100 false
```

## Assumptions and Validation Rules

### Station IDs and Charger IDs
- Must be unsigned 32-bit integers (0 to 4,294,967,295)
- Station IDs must be unique
- Charger IDs must be unique across all stations
- Each station must have at least one charger

### Time Ranges
- Start and end times must be unsigned 64-bit integers (0 to 18,446,744,073,709,551,615)
- End time must be greater than or equal to start time
- Time periods for the same charger cannot overlap
- Adjacent time periods are allowed (one period can end at the same time another begins)
- Gaps between time periods are allowed

### Availability Reports
- Must be in format: `<Charger ID> <start time> <end time> <up status>`
- Up status must be either `true` or `false`
- Reports can only reference charger IDs that exist in the stations section
- Missing reports for chargers are allowed (will generate a warning)

### File Format
- Must contain both `[Stations]` and `[Charger Availability Reports]` sections
- Empty lines are ignored
- At least one station must be defined
- At least one availability report must be provided

## Error Handling

The program will return null (exit with error) if:
1. Input file is not found
2. Invalid integer values (out of range)
3. Invalid boolean values
4. Duplicate station or charger IDs
5. Overlapping time periods
6. Reports for non-existent chargers
7. Invalid file format

Warnings will be generated (but processing continues) for:
1. Missing reports for chargers

## Output

The program outputs a single decimal number representing the uptime percentage across all chargers, rounded to 2 decimal places.

Example:
```bash
$ dotnet run -- input.txt
70.00
``` 