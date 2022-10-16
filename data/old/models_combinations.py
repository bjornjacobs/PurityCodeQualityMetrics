from os import stat
from sklearn.metrics import confusion_matrix, r2_score, precision_score, recall_score, matthews_corrcoef, f1_score
import pandas as pd
from sklearn.linear_model import LogisticRegression
from sklearn.model_selection import KFold, cross_val_predict
import numpy as np
import matplotlib.pyplot as plt
import seaborn as sns
from sklearn.model_selection import train_test_split
import math


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


projects = ['roslyn', 'machinelearning', 'jellyfin', 'akka.net', 'IdentityServer4', 'ILSpy',
            'MoreLINQ', 'Humanizer', 'reactive', 'OpenRA', 'shadowsocks-windows', 'resharper-unity']

models = ['purity', 'baseline-fp', 'baseline-oop']

cols = {
    'purity': ['r-local', 'w-local', 'r-global', 'w-global', 'w-para', 'w-non-fresh'],
    'baseline-fp': ['LambdaCount', 'LambdaScore', 'UnterminatedCollections', 'SourceLinesOfLambda', 'LambdaFieldVariableUsageCount', 'LambdaLocalVariableUsageCount', 'LambdaSideEffectCount'],
    'baseline-oop': ['CommentDensity', 'CyclomaticComplexity', 'DepthOfInheritanceTree', 'LackOfCohesionOfMethods', 'ResponseForAClass', 'WeightedMethodsPerClass'],
}


combinations = [
    (models[2], models[0]),
    (models[2], models[1]),
    (models[0], models[1]),
    (models[0], models[1], models[2]),
]


# def evaluate(pima, feature_cols):
#     X = pima[feature_cols]  # Features
#     y = pima['faulty']  # Target variable

#     X_train, X_test, y_train, y_test = train_test_split(
#         X, y, test_size=0.25, random_state=0)

#     # instantiate the model (using the default parameters)
#     logreg = LogisticRegression(
#         class_weight='balanced', random_state=42, max_iter=10000)
#     logreg.fit(X_train, y_train)

#     y_pred = logreg.predict(X_test)  # cross_val_predict(logreg, X, y, cv=cv)
#     # cross_val_predict(logreg, X, y, cv=cv)
#     y_pred_proba = logreg.predict_proba(X_test)
#     y_pred_proba = np.max(y_pred_proba, axis=1)

#     stats = get_stats(y_test, y_pred)
#     return stats

def evaluate(pima, feature_cols):
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
    for m in combinations:
        if len(m) == 2:
            print(m[0], m[1])
            col_names = cols[m[0]] + cols[m[1]] + ['faulty']
            feature_cols = col_names[:-1]
            pima = pd.read_csv(
                f"regression/combinations/regression-{p}-{m[0]}-{m[1]}.csv", header=None, names=col_names, sep=';')
            stats = evaluate(pima, feature_cols)

            results.append(round(stats['precision']))
            results.append(round(stats['recall']))
            results.append(round(stats['fscore'] * 100))
            print(results)
        if len(m) == 3:
            print(m[0], m[1], m[2])
            col_names = cols[m[0]] + cols[m[1]] + cols[m[2]] + ['faulty']
            feature_cols = col_names[:-1]
            pima = pd.read_csv(
                f"regression/combinations/regression-{p}-{m[0]}-{m[1]}-{m[2]}.csv", header=None, names=col_names, sep=';')
            stats = evaluate(pima, feature_cols)

            results.append(round(stats['precision']))
            results.append(round(stats['recall']))
            results.append(round(stats['fscore'] * 100))
            print(results)

results.append("Overall")
for m in combinations:
    if len(m) == 2:
        col_names = cols[m[0]] + cols[m[1]] + ['faulty']
        feature_cols = col_names[:-1]
        pima = pd.read_csv(
            f"regression/combinations/regression--{m[0]}-{m[1]}.csv", header=None, names=col_names, sep=';')
        stats = evaluate(pima, feature_cols)

        results.append(round(stats['precision']))
        results.append(round(stats['recall']))
        results.append(round(stats['fscore'] * 100))
        print(results)
    if len(m) == 3:
        col_names = cols[m[0]] + cols[m[1]] + cols[m[2]] + ['faulty']
        feature_cols = col_names[:-1]
        pima = pd.read_csv(
            f"regression/combinations/regression--{m[0]}-{m[1]}-{m[2]}.csv", header=None, names=col_names, sep=';')
        stats = evaluate(pima, feature_cols)

        results.append(round(stats['precision']))
        results.append(round(stats['recall']))
        results.append(round(stats['fscore'] * 100))
        print(results)

for x in range(len(projects) + 1):
    for y in range(13):
        if (y == 3 or y == 6 or y == 9 or y == 12 or y == 15):
            print('\\textbf{', results[x * 13 + y], '}', end='&')
        else:
            print(results[x * 13 + y], ' & ',  end='')

    print("\\\\ \\hline")
