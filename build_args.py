
import sys
import os
import argparse
import json
from time import time

def main():
    parser = argparse.ArgumentParser('Stock Cutter Solver')
    parser.add_argument('config', help='Config file to configure EA')
    parser.add_argument('problem', help='Problem file to solve')
    args = parser.parse_args()

    if not os.path.isfile(args.config):
        print('Not a valid path: {}'.format(args.config))
        sys.exit()

    if not os.path.isfile(args.problem):
        print('Not a valid path: {}'.format(args.problem))
        sys.exit()

    with open(args.config) as cf:
        config = json.loads(cf.read())

    arg_parts = [
        args.config,
        args.problem,
        config['solution_file'],
        config['log_file'],
        config.get('seed', int(time())),
        config['runs'],
        config['num_parents'],
        config['num_offspring'],
        config['parent_selection']['selection_weight'],
        config['parent_selection']['select_k'],
        config['parent_selection']['replacement'],
        config['parent_selection']['rate_p'],
        config['parent_selection']['adaptive_crossover'],
        config['parent_selection']['rate_adjacency_crossover'],
        config['survival_selection']['selection_weight'],
        config['survival_selection']['select_k'],
        config['survival_selection']['drop_parents'],
        config['survival_selection']['replacement'],
        config['survival_selection']['rate_p'],
        config['terminations'].get('eval_limit', 0),
        config['terminations'].get('generation_limit', 0),
        config['terminations'].get('unchanged_avg_gen_limit', 0),
        config['terminations'].get('unchanged_best_gen_limit', 0),
        config['mutations']['adaptive'],
        config['mutations']['creep_random'],
        config['mutations']['rotate_random']
    ]
    print(' '.join(map(str, arg_parts)))

if __name__ == '__main__':
    main()
