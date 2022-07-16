from os import stat
from sklearn.metrics import confusion_matrix, r2_score, precision_score, recall_score, matthews_corrcoef, f1_score
import pandas as pd
from sklearn.linear_model import LogisticRegression
from sklearn.model_selection import KFold, cross_val_predict
import numpy as np
import matplotlib.pyplot as plt
import seaborn as sns
from sklearn.model_selection import train_test_split


def get_stats(actual, predicted):
    tn, fp, fn, tp = confusion_matrix(actual, predicted).ravel()
    r2 = r2_score(actual, predicted)
    fscore = f1_score(actual, predicted)
    precision = precision_score(actual, predicted, zero_division=0)
    recall = recall_score(actual, predicted, zero_division=0)
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


projects = ['roslyn', 'machinelearning', 'jellyfin', 'akka.net', 'IdentityServer4', 'ILSpy',
            'MoreLINQ', 'Humanizer', 'reactive', 'OpenRA', 'shadowsocks-windows', 'resharper-unity']


col_names = ['r-local', 'w-local', 'r-global', 'w-global',
             'w-para', 'w-non-fresh', 'faulty']

feature_cols = ['r-local', 'w-local', 'r-global',
                'w-global', 'w-para', 'w-non-fresh']


def evaluate(pima):
    X = pima[feature_cols]  # Features
    y = pima['faulty']  # Target variable

    cv = KFold(n_splits=10, random_state=1, shuffle=True)

    # instantiate the model (using the default parameters)
    model = LogisticRegression(
        class_weight='balanced', random_state=42, max_iter=10000)
    print("started training..")
    y_pred = cross_val_predict(model, X, y, cv=cv)

    stats = get_stats(y, y_pred)

    return stats


results = []
for p in projects:
    results.append(p)
    for i in range(5):
        # load dataset
        pima = pd.read_csv(
            f"regression/purity_metric/regression-{p}-{i}.csv", header=None, names=col_names, sep=';')
        stats = evaluate(pima)
        results.append(round(stats['precision'] * 100))
        results.append(round(stats['recall'] * 100))
        results.append(round(stats['fscore'] * 100))
        print(results)

results.append('total')
for i in range(5):
    pima = pd.read_csv(
        f"regression/purity_metric/regression-{i}.csv", header=None, names=col_names, sep=';')
    stats = evaluate(pima)

    results.append(round(stats['precision'] * 100))
    results.append(round(stats['recall'] * 100))
    results.append(round(stats['fscore'] * 100))
    print(results)

for x in range(len(projects) + 1):
    for y in range(16):
        if (y == 3 or y == 6 or y == 9 or y == 12 or y == 15):
            print('\\textbf{', results[x * 16 + y], '}', end='&')
        else:
            print(results[x * 16 + y], ' & ',  end='')

    print("\\\\ \\hline")
