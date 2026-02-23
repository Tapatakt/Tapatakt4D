# Tapatakt4D Style Guide

## Naming and Types

- **No `var`** unless the name of the class is very long (e.g., you can use var for kvps).
  - Good: `Vector4 position = new Vector4(1.0f, 2.0f, 3.0f, 4.0f);`
  - Bad: `var position = new Vector4(1.0f, 2.0f, 3.0f, 4.0f);`
  - Good: `var kvp = new KeyValuePair<string, VeryLongTypeName>(key, value);`

## Code Structure

- **No unnecessary curly brackets**. Use `=>` in only-return methods.
  - Good: `public int GetCount() => _count;`
  - Bad: `public int GetCount() { return _count; }`

- **Use collection expressions when possible**.
  - Good: `int[] numbers = [1, 2, 3];`
  - Bad: `int[] numbers = new int[] { 1, 2, 3 };`

## Error Handling

- **No silent fails**. Code must throw exception if switch statement encountered something unexpected and not just return default value.
  - Good:
    ```csharp
    switch (value)
    {
        case Option.A: return 1;
        case Option.B: return 2;
        default: throw new ArgumentOutOfRangeException(nameof(value), $"Unexpected value: {value}");
    }
    ```
  - Bad:
    ```csharp
    switch (value)
    {
        case Option.A: return 1;
        case Option.B: return 2;
        default: return 0; // Silent fail!
    }
    ```

## Documentation

- **XML comments for all methods and properties**.
  - Public and internal members must have complete XML documentation.

## Access Modifiers

- **No unnecessary public fields**. If something is not part of the API for user, make it at least internal.
  - Public members are the user-facing API.
  - Internal members are for library implementation details.
  - Private members are for class-internal state only.
  - Use properties instead of public fields.
