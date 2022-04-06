# evaluate multinomial logistic regression model
from numpy import mean
from numpy import std
from numpy import ones, zeros
import numpy as np
from csv import reader
from sklearn.datasets import make_classification
from sklearn.model_selection import cross_val_score
from sklearn.model_selection import RepeatedStratifiedKFold
from sklearn.linear_model import LogisticRegression
from matplotlib import pyplot as plt


# define dataset

x = []
xCount = 0
yCount = 0
with open('wrong.csv', 'r') as read_obj:
    # pass the file object to reader() to get the reader object
    csv_reader = reader(read_obj)
    # Iterate over each row in the csv using reader object
    for row in csv_reader:
        n = float(row[0]) + 1
        x.append(np.array(list(map(lambda a: float(a) / n, row[1:]))))
        xCount += 1

with open('right-big.csv', 'r') as read_obj:
    # pass the file object to reader() to get the reader object
    csv_reader = reader(read_obj)
    csv_reader
    # Iterate over each row in the csv using reader object
    for row in csv_reader:
        n = float(row[0]) + 1
        x.append(np.array(list(map(lambda a: float(a) / n, row[1:]))))
        yCount += 1
99
y = ([1] * xCount) + ([0] * yCount)

x = np.array(x)
print(x)


# define the multinomial logistic regression model
model = LogisticRegression(multi_class='multinomial',
                           solver='lbfgs', max_iter=1000)
# define the model evaluation procedure
cv = RepeatedStratifiedKFold(
    n_splits=10, n_repeats=10, random_state=1)
# evaluate the model and collect the scores

n_scores = cross_val_score(model, x, y, scoring='accuracy', cv=cv, n_jobs=-1)
# report the model performance
print('Mean Accuracy: %.3f (%.3f)' % (mean(n_scores), std(n_scores)))
