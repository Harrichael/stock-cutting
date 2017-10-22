
import sys
import json
from time import time

def main():
    config_file, problem_file = sys.argv[1:]
    with open(config_file) as cf:
        config = json.loads(cf.read())

    arg_parts = [
        config_file,
        problem_file,
        config['solution_file'],
        config['log_file'],
        config.get('seed', int(time())),
        config['runs'],
        config['num_parents'],
        config['num_offspring'],
        config['parent_selection']['selection_weight'],
        config['parent_selection']['select_k'],
        config['parent_selection']['num_mates'],
        config['parent_selection']['replacement'],
        config['parent_selection']['rate_p'],
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
        config['mutations']['rate_per_offspring'],
        config['mutations']['creep_random'],
        config['mutations']['creep_stable_random'],
        config['mutations']['swap_position'],
        config['mutations']['swap_insertion'],
        config['force_valid'],
        config['penalty_weight'],
        config['adaptive_penalty'],
        config['adaptive_repair'],
        config['init_repair_chance'],
    ]
    print(' '.join(map(str, arg_parts)))

if __name__ == '__main__':
    main()
