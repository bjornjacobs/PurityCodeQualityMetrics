from os import stat
from sklearn.metrics import confusion_matrix, r2_score, precision_score, recall_score, matthews_corrcoef, f1_score
import pandas as pd
from sklearn.linear_model import LogisticRegression
from sklearn.model_selection import KFold, cross_val_predict
import numpy as np
import matplotlib.pyplot as plt
import seaborn as sns
from sklearn.model_selection import train_test_split


col_names = ['r-local', 'w-local', 'r-global', 'w-global',
             'w-para', 'w-non-fresh', 'faulty']


projects = ['roslyn', 'machinelearning', 'jellyfin', 'akka.net', 'IdentityServer4', 'ILSpy',
            'MoreLINQ', 'Humanizer', 'reactive', 'OpenRA', 'shadowsocks-windows', 'resharper-unity']

c = 0

for p in projects:
    pima = pd.read_csv(f'regression/purity_metric/regression-{p}-3.csv',
                       header=None, names=col_names, sep=';')

  #  if(p == 'roslyn'):
  #      pima = pima.sample(n=40000, random_state=42)

    total = len(pima['faulty'])

    c = c + total
    faulty = len(pima[pima['faulty'] == 1])
    nonfaulty = len(pima[pima['faulty'] == 0])
    print(p, total, faulty, nonfaulty)

print(c / 100)
