# RPNCalc Library

## Intro

Extensible calculation library with flexibility of Reverse Polish Notation (RPN). It is designed to handle not only numbers, but also strings, lists, programs, and user-defined functions, making it useful for scripting, expression evaluation, and embedding programmable logic into applications.

> Unless noted otherwise, everything described here applies to **RPN mode**.  
> Not all functionality is supported in Algebraic mode.

## Notation

This library supports two types of notation:

- [Reverse Polish Notation (RPN)](https://en.wikipedia.org/wiki/Reverse_Polish_notation)
- [Algebraic expressions](https://en.wikipedia.org/wiki/Algebraic_expression)

RPN is the primary mode to access the full functionality of the library. Algebraic mode is secondary and, due to the internal stack design, can sometimes be written in a **hybrid form**. In such cases, values may be pushed onto the stack, and missing function parameters are automatically taken from the stack.

**Examples** (all produce the result `1`):

- Standard algebraic syntax:  
  ```contain(['foo','bar'], 'foo')```
- Hybrid algebraic syntax:  
  ```['foo','bar'] contain('foo')```
- Almost RPN, but still valid in algebraic mode:  
  ```['foo','bar'] 'foo' contain()```

## Supported Data Types

- **Numbers**
  - Real: `42`, `(-3.33)`, `13.37e2`, `31415e-4`, `.1`  
  - Complex: `( 10 20 )`, `( 1 -3e-26 )`  
    *(spaces around brackets are required)*

- **Strings**  
  `'foobar'`, `'don\'t panic'`  
  *(escape apostrophes with a backslash)*

- **Lists**  
  `[ 42 ( 1 2 ) 'item' { dup * } cos [ 1 2 3 ] ]`  
  *(spacing is significant)*

- **Programs**  
  `{ 42 == { 'don\'t panic' } { 'keep panicking' } IFTE }`  
  *(spacing rules apply)*  
  equivalent to:

  ```csharp
  (int x) => {
      if (x == 42) return "don't panic";
      else return "keep panicking";
  }
  ```

- **Functions**
  - Can only be defined from C#.
  - Functions and programs are conceptually similar:
    - A **function** is typically a C# method.
    - A **program** is a sequence of RPN instructions that is interpreted at runtime (generally slower).

### Notes on Algebraic Syntax

- All basic math operations should work as expected.
- Supported infix operators: `+ - * / ^`
- Example of a function calls:
  - ```some_function(variableXY, 42, 'a string parameter')```
  - ```funcA(funcB(123)+3,variable.X=='item')```

Unlike RPN mode, **spaces are not required** in algebraic mode and can be omitted.

## Built-in Functions

A selection of available functions (not exhaustive):

```text
+         addition
-         subtraction
*         multiplication
/         division
^         power
+-        change sign
++        increment variable
--        decrement variable
1/X       reciprocal
SQ        square
SQRT      square root
DROP      remove top stack value
DUP       duplicate top stack value
SWAP      swap top two values
DEPTH     stack depth
ROT       rotate top three values
ROLL      roll entire stack
OVER      duplicate second value
CLST      clear stack
CLV       clear value
EVAL      evaluate program or string
STO       store variable
RCL       recall variable
RND       round to X decimal places
RND0      round to integer
IP        integer part
FP        fractional part
FLOOR     floor
CEIL      ceiling
IFT       IF-THEN
IFTE      IF-THEN-ELSE
WHILE     WHILE loop
FOR       FOR loop
LOOP      simple numeric loop
==        equality
!=        inequality
<         less than
<=        less or equal
>         greater than
>=        greater or equal
HEAD      first element (list/string)
TAIL      all except first (list/string)
CONTAIN   contains
>LIST     collect stack values into a list
LIST>     expand list values onto stack
