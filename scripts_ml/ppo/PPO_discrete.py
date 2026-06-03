import torch
import torch.nn as nn
from torch.distributions import Categorical
import gym
import numpy as np
import os
import argparse

parser = argparse.ArgumentParser(description='PyTorch PPO for discrete control')
parser.add_argument('--env', type=str, default='LunarLander-v2', help='Environment name')
parser.add_argument('--lr', type=float, default=0.002, help='Learning rate')
parser.add_argument('--gamma', type=float, default=0.99, help='Discount factor')
parser.add_argument('--max_episodes', type=int, default=100000)
parser.add_argument('--ckpt_folder', default='./checkpoints', help='Model checkpoints folder')
opt = parser.parse_args()

device = torch.device("cuda:0" if torch.cuda.is_available() else "cpu")

class Memory:   
    def __init__(self):
        self.states = []
        self.actions = []
        self.rewards = []
        self.is_terminals = []
        self.logprobs = []

    def clear_memory(self):
        del self.states[:]
        del self.actions[:]
        del self.rewards[:]
        del self.is_terminals[:]
        del self.logprobs[:]

class ActorCritic(nn.Module):
    def __init__(self, state_dim, action_dim):
        super(ActorCritic, self).__init__()
        self.actor = nn.Sequential(
            nn.Linear(state_dim, 64),
            nn.Tanh(),
            nn.Linear(64, 32),
            nn.Tanh(),
            nn.Linear(32, action_dim),
            nn.Softmax(dim=-1)			
        )
        self.critic = nn.Sequential(
            nn.Linear(state_dim, 64),
            nn.Tanh(),
            nn.Linear(64, 32),
            nn.Tanh(),
            nn.Linear(32, 1)
        )

    def act(self, state, memory):       		
        action_probs = self.actor(state)        
        dist = Categorical(action_probs)		
        action = dist.sample()          		
        action_logprob = dist.log_prob(action)  

        memory.states.append(state)
        memory.actions.append(action)
        memory.logprobs.append(action_logprob)
        return action.item()	

    def evaluate(self, state, action):      
        state_value = self.critic(state)    
        action_probs = self.actor(state)    
        dist = Categorical(action_probs)
        action_logprobs = dist.log_prob(action)	
        dist_entropy = dist.entropy()
        return action_logprobs, torch.squeeze(state_value), dist_entropy

class PPO:
    def __init__(self, state_dim, action_dim, lr, betas, gamma, K_epochs, eps_clip):
        self.gamma = gamma
        self.eps_clip = eps_clip
        self.K_epochs = K_epochs
        self.policy = ActorCritic(state_dim, action_dim).to(device)
        self.optimizer = torch.optim.Adam(self.policy.parameters(), lr=lr, betas=betas)
        self.old_policy = ActorCritic(state_dim, action_dim).to(device)
        self.old_policy.load_state_dict(self.policy.state_dict())
        self.MSE_loss = nn.MSELoss()	

    def select_action(self, state, memory):
        state = torch.FloatTensor(state.reshape(1, -1)).to(device)  
        return self.old_policy.act(state, memory)

    def update(self, memory):
        rewards = []
        discounted_reward = 0
        for reward, is_terminal in zip(reversed(memory.rewards), reversed(memory.is_terminals)):
            if is_terminal:
                discounted_reward = 0
            discounted_reward = reward + self.gamma * discounted_reward
            rewards.insert(0, discounted_reward)

        rewards = torch.tensor(rewards).to(device)
        rewards = (rewards - rewards.mean()) / (rewards.std() + 1e-5)

        old_states = torch.squeeze(torch.stack(memory.states).to(device)).detach()
        old_actions = torch.squeeze(torch.stack(memory.actions).to(device)).detach()
        old_logprobs = torch.squeeze(torch.stack(memory.logprobs)).to(device).detach()

        for _ in range(self.K_epochs):
            logprobs, state_values, dist_entropy = self.policy.evaluate(old_states, old_actions)
            ratios = torch.exp(logprobs - old_logprobs.detach())
            advantages = rewards - state_values.detach()  
            surr1 = ratios * advantages
            surr2 = torch.clamp(ratios, 1 - self.eps_clip, 1 + self.eps_clip) * advantages
            actor_loss = - torch.min(surr1, surr2)
            critic_loss = 0.5 * self.MSE_loss(rewards, state_values) - 0.01 * dist_entropy
            loss = actor_loss + critic_loss

            self.optimizer.zero_grad()
            loss.mean().backward()
            self.optimizer.step()
        
        self.old_policy.load_state_dict(self.policy.state_dict())

def train():
    print("Starting training on Environment: {}".format(opt.env))
    # ... training loop placeholder ...

if __name__ == '__main__':
    if not os.path.exists(opt.ckpt_folder):
        os.makedirs(opt.ckpt_folder)
    train()
