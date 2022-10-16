from email import header
from glob import glob
from os import stat
from statistics import variance
from tkinter import Variable
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


feature_cols = ['r-local', 'w-local', 'r-global',
                'w-global', 'w-para', 'w-non-fresh']


training_projects = ['Roslyn', 'akka.net',
                     'ILSpy', 'reactive']

validation_projects = ['shadowsocks-windows',
                       'resharper-unity', 'Humanizer', 'OpenRA']

test_projects = ['machinelearning', 'jellyfin',
                 'MoreLINQ', 'Identityserver4']


#test_projects = validation_projects

col_names = ['r-local', 'w-local', 'r-global', 'w-global',
             'w-para', 'w-non-fresh', 'faulty']


models = ['purity', 'baseline-fp', 'baseline-oop']


variable = ['2']


variable = ['0', '1', '2', '3', '4']

variable = ['2', 'baseline-fp', 'baseline-oop']

variable = [
    #(models[2], models[0]),
    #(models[2], models[1]),
    # (models[0], models[1]),
    (models[0], models[1], models[2]),
]


variable = ['Lambda', 'Local', 'Method']

for v in variable:

    # def readData(p, full):

    #     fn = f'regression/purity_metric/regression-{p}-{v}.csv'

    #     df = pd.read_csv(fn,
    #                      header=None, names=col_names, sep=';')
    #     if(len(df) > 40000):
    #         df = df.sample(n=40000, random_state=42)
    #     return df

    # Contructs
    def readData(p, full):
        fn = f'regression/constructs/regression-{p}-2-{v}.csv'

        if full is True:
            fn = f'regression/purity_metric/regression-{p}-3.csv'

        df = pd.read_csv(fn,
                         header=None, names=col_names, sep=';')
        if(len(df) > 40000):
            df = df.sample(n=40000, random_state=42)
        return df

    # Models
    # def readData(p, full):
    #     global col_names
    #     global feature_cols
    #     if(v == '2'):
    #         fn = f'regression/purity_metric/regression-{p}-{v}.csv'
    #         col_names = ['r-local', 'w-local', 'r-global', 'w-global',
    #                      'w-para', 'w-non-fresh', 'faulty']
    #     elif(v == 'baseline-fp'):
    #         fn = f'regression/models/regression-{v}-{p}.csv'
    #         col_names = ['LambdaCount', 'LambdaScore', 'UnterminatedCollections', 'SourceLinesOfLambda',
    #                      'LambdaFieldVariableUsageCount', 'LambdaLocalVariableUsageCount', 'LambdaSideEffectCount', 'faulty']
    #     elif(v == 'baseline-oop'):
    #         fn = f'regression/models/regression-{v}-{p}.csv'
    #         col_names = ['CommentDensity', 'CyclomaticComplexity', 'DepthOfInheritanceTree',
    #                      'LackOfCohesionOfMethods', 'ResponseForAClass', 'WeightedMethodsPerClass',  'faulty']

    #     df = pd.read_csv(fn,
    #                      header=None, names=col_names, sep=';')

    #     feature_cols = col_names[:-1]
    #     if(len(df) > 40000):
    #         df = df.sample(n=40000, random_state=42)
    #     return df

    # def readData(p, full):
    #     global col_names
    #     global feature_cols
    #     if(v[0] == 'purity'):
    #         fn = f'regression/purity_metric/regression-{p}-{v[0]}.csv'
    #         col_names = ['r-local', 'w-local', 'r-global', 'w-global',
    #                      'w-para', 'w-non-fresh', 'faulty']
    #     elif(v[0] == 'baseline-fp'):
    #         fn = f'regression/models/regression-{v[0]}-{p}.csv'
    #         col_names = ['LambdaCount', 'LambdaScore', 'UnterminatedCollections', 'SourceLinesOfLambda',
    #                      'LambdaFieldVariableUsageCount', 'LambdaLocalVariableUsageCount', 'LambdaSideEffectCount', 'faulty']
    #     elif(v[0] == 'baseline-oop'):
    #         fn = f'regression/models/regression-{v[0]}-{p}.csv'
    #         col_names = ['CommentDensity', 'CyclomaticComplexity', 'DepthOfInheritanceTree',
    #                      'LackOfCohesionOfMethods', 'ResponseForAClass', 'WeightedMethodsPerClass',  'faulty']

    #     if(v[1] == 'purity'):
    #         fn2 = f'regression/purity_metric/regression-{p}-{v[1]}.csv'
    #         col_names = col_names[:-1] + ['r-local', 'w-local', 'r-global', 'w-global',
    #                                       'w-para', 'w-non-fresh', 'faulty']
    #     elif(v[1] == 'baseline-fp'):
    #         fn2 = f'regression/models/regression-{v[1]}-{p}.csv'
    #         col_names = col_names[:-1] + ['LambdaCount', 'LambdaScore', 'UnterminatedCollections', 'SourceLinesOfLambda',
    #                                       'LambdaFieldVariableUsageCount', 'LambdaLocalVariableUsageCount', 'LambdaSideEffectCount', 'faulty']
    #     elif(v[1] == 'baseline-oop'):
    #         fn2 = f'regression/models/regression-{v[1]}-{p}.csv'
    #         col_names = col_names[:-1] + ['CommentDensity', 'CyclomaticComplexity', 'DepthOfInheritanceTree',
    #                                       'LackOfCohesionOfMethods', 'ResponseForAClass', 'WeightedMethodsPerClass',  'faulty']

    #     # if(len(v) == 3):
    #     if(v[2] == 'purity'):
    #         fn2 = f'regression/purity_metric/regression-{p}-{v[2]}.csv'
    #         col_names = col_names[:-1] + ['r-local', 'w-local', 'r-global', 'w-global',
    #                                       'w-para', 'w-non-fresh', 'faulty']
    #     elif(v[2] == 'baseline-fp'):
    #         fn2 = f'regression/models/regression-{v[2]}-{p}.csv'
    #         col_names = col_names[:-1] + ['LambdaCount', 'LambdaScore', 'UnterminatedCollections', 'SourceLinesOfLambda',
    #                                       'LambdaFieldVariableUsageCount', 'LambdaLocalVariableUsageCount', 'LambdaSideEffectCount', 'faulty']
    #     elif(v[2] == 'baseline-oop'):
    #         fn2 = f'regression/models/regression-{v[2]}-{p}.csv'
    #         col_names = col_names[:-1] + ['CommentDensity', 'CyclomaticComplexity', 'DepthOfInheritanceTree',
    #                                       'LackOfCohesionOfMethods', 'ResponseForAClass', 'WeightedMethodsPerClass',  'faulty']

    #     df = pd.read_csv(f"regression/combinations/regression-{p}-{v[0]}-{v[1]}-{v[2]}.csv",
    #                      header=None, names=col_names, sep=';')
    #     feature_cols = col_names[:-1]
    #     if(len(df) > 40000):
    #         df = df.sample(n=40000, random_state=42)
    #     return df

    train = pd.DataFrame()
    test = pd.DataFrame()
    for p in training_projects:
        # load dataset
        df = readData(p, True)

        train = pd.concat([train, df],  names=feature_cols)

    for p in test_projects:
        # load dataset
        df = readData(p, False)

        test = pd.concat([test, df], names=feature_cols)

    X_train = train[feature_cols]  # Features
    y_train = train['faulty'].astype('int')  # Target variable

    X_test = test[feature_cols]  # Features
    y_test = test['faulty'].astype('int')  # Target variable

    logreg = LogisticRegression(class_weight='balanced',
                                random_state=42, max_iter=10000)
    logreg.fit(X_train, y_train)

    y_pred = logreg.predict(X_test)  # cross_val_predict(logreg, X, y, cv=cv)
    stats = get_stats(y_test, y_pred)

    print(v)
    print("Precision", stats['precision'])
    print("Recall", stats["recall"])
    print("Fscore", stats["fscore"])
