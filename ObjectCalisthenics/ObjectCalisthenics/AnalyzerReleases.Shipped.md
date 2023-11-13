## Release 1.0

### New Rules

| Rule ID | Category    | Severity | Notes                                                                                                        |
|---------|-------------|----------|--------------------------------------------------------------------------------------------------------------|
| OC0001  | Readability | Warning  | Methods should only have one level of indentation to improve readability                                     |
| OC0002  | Size        | Warning  | Do not use the `else` keyword to encourage single entry, single exit (SESE) functions                        |
| OC0003  | Size        | Warning  | Wrap all primitives and strings to give them additional behavior and meaning                                 |
| OC0004  | Size        | Warning  | Use only one dot per line to avoid the Law of Demeter violation, encourages encapsulation                    |
| OC0005  | Names       | Warning  | Do not abbreviate when naming variables, it should be clear and unambiguous                                  |
| OC0006  | Size        | Warning  | Keep your entities small, classes should be less than 50 lines and packages should have less than 10 files   |
| OC0007  | Complexity  | Warning  | Limit the number of instance variables in a class to encourage more cohesive, less coupled classes           |
| OC0008  | Complexity  | Warning  | Use first-class collections, any class that contains a collection should contain no other member variables   |
| OC0009  | Testing     | Warning  | Do not use getters/setters/properties, which exposes internal implementation and makes it harder to refactor |
