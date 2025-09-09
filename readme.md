
---

# **Nytric Language Developer Guide

---

## **1. Introduction**

Nytric is a **prefix-style, dynamically typed scripting language** with:

* Variables
* Math and string operations
* Conditionals and loops
* Functions
* Built-in utilities: `SQRT`, `RANDOM`, `REVERSE`, `LEN`
* Pure prefix syntax — no parentheses required
* Blocks delimited by `{ ... }`
* Easy extensibility for adding new operators or statements

Nytric runs through a **C# interpreter**, and scripts use the `.ncs` extension.

---

## **2. Variables**

Variables are declared with `let`:

```nytric
let x = 10
let name = "Nytric"
```

* Variables are dynamically typed (numbers or strings)
* Scope: Variables exist in the **current scope** (global or function-local)
* Access variables anywhere in their scope:

```nytric
let greeting = "Hello"
say greeting  // prints: Hello
```

---

## **3. Operators**

### **3.1 Math Operators (Binary)**

Prefix notation: `<operator> <operand1> <operand2>`

```nytric
print + 3 4       // 7
print - 10 5      // 5
print * 2 3       // 6
print / 10 2      // 5
```

* If operands are strings, only `+` is valid (concatenation):

```nytric
say + "Hello, " name   // Hello, Nytric
```

### **3.2 Unary Operators**

| Operator  | Description                   |
| --------- | ----------------------------- |
| `SQRT`    | Square root                   |
| `RANDOM`  | Random number between 0 and N |
| `REVERSE` | Reverse string                |
| `LEN`     | Length of string              |

Examples:

```nytric
print SQRT 16       // 4
print RANDOM 100    // e.g., 42.37
print REVERSE name  // "cirtyN"
print LEN name      // 6
```

---

## **4. Expressions**

* Nytric uses **pure prefix notation**
* Nested expressions are written without parentheses:

```nytric
+ add 3 4 square 2   // evaluates as add(3,4) + square(2)
```

**Important:** Parentheses are **not allowed**. Use nesting directly after operators.

---

## **5. Conditionals**

Syntax:

```nytric
if <condition>
{
    // statements
}
else
{
    // optional else block
}
```

Examples:

```nytric
if - y x
{
    print "y is greater than x"
}
else
{
    print "x is greater or equal to y"
}

// Single-statement block without braces
if greeting
    print "greeting is non-empty"
```

* Conditions are truthy if:

  * Numbers: non-zero
  * Strings: non-empty

---

## **6. Loops**

### **6.1 While Loop**

```nytric
let i = 1
while - 5 i
{
    print i
    let i = + i 1
}
```

### **6.2 For Loop**

```nytric
for j = 1 5
{
    print j
}
```

---

## **7. Functions**

Declare functions:

```nytric
function add(a, b)
{
    return + a b
}

function square(n)
{
    return * n n
}
```

Call functions:

```nytric
print add 7 8      // 15
print square 6     // 36

say + "Hi " add 3 4
```

* Functions have **local scope**
* `return` ends function execution
* Nested function calls follow prefix notation

---

## **8. Built-in Utilities**

| Function  | Description      | Example                              |
| --------- | ---------------- | ------------------------------------ |
| `SQRT`    | Square root      | `print SQRT 25 // 5`                 |
| `RANDOM`  | Random 0–N       | `print RANDOM 100`                   |
| `REVERSE` | Reverse string   | `print REVERSE "Nytric" // "cirtyN"` |
| `LEN`     | Length of string | `print LEN "Nytric" // 6`            |

---

## **9. Combining Features**

```nytric
// Nested math and functions
let total = + add 3 4 square 2
print total  // 11

// Combined string operations
say REVERSE + "Hello, " name  // "cirtyN ,olleH"
```

---

## **10. Error Handling**

* Division by zero throws a runtime error
* Using unknown variable/function throws a runtime error
* Unary operators on invalid types will attempt coercion (strings for REVERSE/LEN)

---

## **11. Adding New Features / Extending Nytric**

### **11.1 Adding a new operator**

1. **Lexer:** Add regex and token type:

```csharp
(TokenType.NEWOP, new Regex(@"^(NEWOP)"))
```

2. **Parser:** Extend `ParseExpression()` to handle new token type:

```csharp
if (tok.Type == TokenType.NEWOP)
{
    var op = Consume(tok.Type).Value;
    var val = ParseExpression(); // unary example
    return new Dictionary<string, object> { { "type", "UnaryStatement" }, { "operator", op }, { "value", val } };
}
```

3. **Interpreter:** Extend `EvaluateUnary` (or `EvaluateMath`):

```csharp
case "NEWOP":
    return SomeComputation(EvaluateExpression(node["value"]));
```

---

### **11.2 Adding a new statement**

* Update `Lexer` with a token for the keyword
* Add a `Parse<Statement>()` in the parser
* Add a case in `Interpreter.Execute()` to handle execution

---

### **11.3 Adding global functions**

* Pre-register them in `_functions` dictionary in `Interpreter` if you want built-in functions
* Or allow scripts to declare functions normally

---

### **11.4 Extending the scope system**

* `_scopes` is a stack of dictionaries
* Add new scopes for loops, functions, or custom blocks
* Always push/pop scopes to manage variable lifetime

---

## **12. Full Example Script**

```nytric
let x = 10
let y = 25
let greeting = "Hello, Nytric!"
let name = "Nytric"

print x
print y
say greeting

print + x y
print SQRT 16
print RANDOM 100
print REVERSE greeting
print LEN greeting

if - y x
{
    print "y > x"
}
else
{
    print "x >= y"
}

function add(a, b)
{
    return + a b
}

print add 5 6

let total = + add 3 4 square 2
print total

say + "Hi " add 1 2
```

Expected output (numbers for `RANDOM` will vary):

```
10
25
Hello, Nytric!
35
4
42.73
!cirtyN ,olleH
15
y > x
11
Hi 3
```

---

### ✅ **13. Best Practices**

* Keep expressions in **prefix notation**
* Use `{ ... }` for multi-statement blocks
* Use `let` for variable declarations inside loops/functions
* Use `return` to exit functions with a value
* All operators/functions are case-sensitive

---

