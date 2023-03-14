# Purity code quality metrics
Master thesis project for software science master at the Radboud University. The full thesis can be found (here)[https://research.infosupport.com/wp-content/uploads/Master_thesis_bjorn_jacobs_1.6.1.pdf]. This repository contains the code and data.

## Abstract 
In software engineering, there is a focus on creating quality code. However, what ex-
actly quality code is and how this is measured is not a trivial question. There are
software metrics that try to capture this quality. These metrics can be programming
language agnostic or focused on a specific programming paradigm like object-oriented or
functional. In recent years more traditional object-oriented languages introduced more
functional features. Recent research evaluated how well existing object-oriented metrics
work on this new multi-paradigm code. Furthermore, new metrics have been defined
with a focus on multi-paradigm code. This thesis will continue with this research by
using one of the fundamental principles of functional programming, namely functional
purity, as a metric. To achieve this we need a way to calculate a purity metric from
csharp code. For this, we combined multiple methods from different studies to create a
purity metric that captures a function’s purity. We evaluated this purity metric against
existing object-oriented and functional metrics. In our testing, it performed better at
predicting error-prone code than existing object oriented or functional metrics.

# Acknowledgement
* [Csharp code quality metrics](https://github.com/bzuilhof/StaticCodeAnalysis)
* [CsPurity](https://github.com/melkster/CsPurity)
