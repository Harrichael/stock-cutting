
from pylab import *
import matplotlib.ticker as ticker
from collections import defaultdict
import argparse

class Generation:
    def __init__(self, best_fitness, avg_fitness):
        self.best_fitness = best_fitness
        self.avg_fitness  = avg_fitness

def save_box_plot(data, image_name, window_size, fit_name):
    fig, ax = plt.subplots(1,1)
    boxplot(data)
    title('Fitness Over the Generations, {}'.format(image_name[6:-4]))
    ylabel('Fitness {}'.format(fit_name))
    xlabel('Evaluation Bucket (bucket size = {} evals)'.format(window_size))

    for index, label in enumerate(ax.get_xticklabels()):
        if index % 2 != 0:
            label.set_visible(False)
    savefig(image_name)

def stepify_data(ys):
    y = []
    for index, y_data in enumerate(ys):
        y_data = list(y_data)
        y_val = max(y_data)
        if len(y) != 0 and y_val < y[-1]:
            y_val = y[-1]
        y.append(y_val)

    return y

def save_step_plot(raw_data, image_name, window_size, fit_name):
    fix, ax = plt.subplots(1,1)
    best_data = condense_data(raw_data, window_size, lambda g: g.best_fitness)
    
    step(
            list(range(0, len(best_data))),
            stepify_data(best_data)
    )
    data = condense_data(raw_data, window_size, lambda g: g.avg_fitness)
    boxplot(data)

    title('All Time Best Fitness, Average Fitness Over the Generations ({})'.format(fit_name))
    ylabel('Fitness {}'.format(fit_name))
    xlabel('Evaluation Bucket (bucket size = {} evals)'.format(window_size))
    for index, label in enumerate(ax.get_xticklabels()):
        if index % 2 != 0:
            label.set_visible(False)
    savefig(image_name)

def condense_data(raw_data, window_size, f):
    data = []
    for evals, values in sorted(raw_data.items(), key=lambda kv: kv[0]):
        if evals > len(data) * window_size:
            data.append([])
        data[-1].extend([f(v) for v in values])

    return data

def parse_data_dict(result_filename, col_num):
    data = defaultdict(list) # Key: evals, Value: [Best Fitness]
    with open(result_filename) as result_file:

        capture_results = False

        for line in result_file.readlines():
            line = line.strip()
            if line == '':
                capture_results = False
            if capture_results:
                evals, *avg_best = map(float, line.split())
                avg_fitness, best_fitness = avg_best[2*col_num : 2 + 2*(col_num)]
                data[evals].append(Generation(best_fitness, avg_fitness))
            else:
                if line.startswith('[Run '):
                    capture_results = True

    return data

def main():
    parser = argparse.ArgumentParser('Create plots for a results file')
    parser.add_argument('result_file', help='Result file to parse and create result file for')
    parser.add_argument('num_col', type=int, help='Fitness to select')
    parser.add_argument('fit_name', help='name of fitness to generate graph on')
    parser.add_argument('-w', '--window_size', default=250, type=int, help='Result file to parse and create result file for')

    args = parser.parse_args()

    raw_data = parse_data_dict(args.result_file, args.num_col)
    windowed_data = condense_data(raw_data, args.window_size, lambda g: g.best_fitness)
    save_box_plot(windowed_data, 'plots/' + args.result_file.replace('.txt', '_box.png'), args.window_size, args.fit_name)
    save_step_plot(raw_data, 'plots/' + args.result_file.replace('.txt', '_step.png'), args.window_size, args.fit_name)

if __name__ == '__main__':
    main()

