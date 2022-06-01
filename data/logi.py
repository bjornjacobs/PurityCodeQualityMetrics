from sklearn.metrics import confusion_matrix, r2_score, precision_score, recall_score, matthews_corrcoef, f1_score
import pandas as pd
from sklearn.linear_model import LogisticRegression
from sklearn.model_selection import KFold, cross_val_predict
import numpy as np
import matplotlib.pyplot as plt
import seaborn as sns


def get_stats(actual, predicted):
    tn, fp, fn, tp = confusion_matrix(actual, predicted).ravel()
    r2 = r2_score(actual, predicted)
    fscore = f1_score(actual, predicted)
    precision = precision_score(actual, predicted, zero_division=0) * 100
    recall = recall_score(actual, predicted, zero_division=0) * 100
    mcc = matthews_corrcoef(actual, predicted)
    return {
        'tn': tn,
        'fp': fp,
        'fn': fn,
        'tp': tp,
        'r2': r2,
        'fscore': fscore,
        'precision': precision,
        'recall': recall,
        'mcc': mcc,
    }


col_names = ['r-local', 'w-local', 'r-global', 'w-global',
             'w-para', 'w-non-fresh', 'faulty']
# load dataset
pima = pd.read_csv("regression/regression.csv", header=None, names=col_names)
feature_cols = ['r-local', 'w-local', 'r-global',
                'w-global', 'w-para', 'w-non-fresh']

print("loaded data.")

X = pima[feature_cols]  # Features
y = pima['faulty']  # Target variable

cv = KFold(n_splits=10, random_state=1, shuffle=True)

# instantiate the model (using the default parameters)
model = LogisticRegression(class_weight='balanced', random_state=42)
print("started training..")
y_pred = cross_val_predict(model, X, y, cv=cv)

stats = get_stats(y, y_pred)

print("Precission", stats['precision'])
print("Recall", stats["recall"])
print("Fscore", stats["fscore"])
