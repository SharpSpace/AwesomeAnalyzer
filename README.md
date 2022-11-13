# AwesomeAnalyzer

AwesomeAnalyzer is a pack of different Roslyn Analyzers

## Installation

Download [latest version here](https://github.com/SharpSpace/AwesomeAnalyzer/releases/download/v0.35.1/AwesomeAnalyzer.0.35.1.vsix)
or on [VisualStudio Marketplace](https://marketplace.visualstudio.com/items?itemName=SharpSpace.AwesomeAnalyzer)

## Analyzers

ID | Notes
--------|----------
[JJ0001](docs/JJ0001.md) | Class should have modifier sealed.
[JJ0003](docs/JJ0003.md) | Variable can be a const.
[JJ0004](docs/JJ0004.md) | Statement is missing using because type implements IDisposable.
[JJ0100](docs/JJ0100.md) | Method contains Async prefix.
[JJ0101](docs/JJ0101.md) | Method call is missing Await.
[JJ0102](docs/JJ0101.md) | Method name is missing Async prefix.
[JJ1001-JJ1013](docs/JJ1001-JJ1013.md) | [Type] needs to be sorted alphabetically. [Type] needs to be in correct order.