# AwesomeAnalyzer

AwesomeAnalyzer is a pack of different Roslyn Analyzers

[![CodeQL](https://github.com/SharpSpace/AwesomeAnalyzer/actions/workflows/codeql.yml/badge.svg)](https://github.com/SharpSpace/AwesomeAnalyzer/actions/workflows/codeql.yml)

## Installation

Download [latest version here](https://github.com/SharpSpace/AwesomeAnalyzer/releases/download/v0.39.0/AwesomeAnalyzer.0.39.0.vsix)
or on [VisualStudio Marketplace](https://marketplace.visualstudio.com/items?itemName=SharpSpace.AwesomeAnalyzer)

## Analyzers

ID | Notes
--------|----------
[JJ0001](https://github.com/SharpSpace/AwesomeAnalyzer/blob/master/Docs/JJ0001.md) | Class should have modifier sealed.
[JJ0003](https://github.com/SharpSpace/AwesomeAnalyzer/blob/master/Docs/JJ0003.md) | Variable can be a const.
[JJ0004](https://github.com/SharpSpace/AwesomeAnalyzer/blob/master/Docs/JJ0004.md) | Statement is missing using because type implements IDisposable.
[JJ0005](https://github.com/SharpSpace/AwesomeAnalyzer/blob/master/Docs/JJ0005.md) | Convert string to [Type].TryParse()
[JJ0006](https://github.com/SharpSpace/AwesomeAnalyzer/blob/master/Docs/JJ0006.md) | Remove async and await in method.
[JJ0100](https://github.com/SharpSpace/AwesomeAnalyzer/blob/master/Docs/JJ0100.md) | Method contains Async prefix.
[JJ0101](https://github.com/SharpSpace/AwesomeAnalyzer/blob/master/Docs/JJ0101.md) | Method call is missing Await.
[JJ0102](https://github.com/SharpSpace/AwesomeAnalyzer/blob/master/Docs/JJ0101.md) | Method name is missing Async prefix.
[JJ1001-JJ1013](https://github.com/SharpSpace/AwesomeAnalyzer/blob/master/Docs/JJ1001-JJ1013.md) | [Type] needs to be sorted alphabetically. [Type] needs to be in correct order.