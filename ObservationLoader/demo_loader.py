#!/usr/bin/env python3

# A basic demo script that loads the array in bom_product_list.json
# and performs a data load

import json
from multiprocessing import Pool
import subprocess
from time import sleep
from random import randrange

with open('bom_product_list.json') as f:
    obs_products = json.load(f)


def job(product):
    out = subprocess.Popen(['dotnet', 'run',
                            "Logging:LogLevel:Default=Debug",
                            "observations:ObservationService:Product={}".format(product)],
                           stdout=subprocess.PIPE,
                           stderr=subprocess.PIPE)
    stdout, stderr = out.communicate()
    print('--')
    print("Product: {}".format(product))
    print(stdout.decode("utf-8"))
    print(stderr.decode("utf-8"))


with Pool() as pool:
    pool.map(job, obs_products)
