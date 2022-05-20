import torch
from torch import nn
from torchvision import models
import json
import os


class Decoder(nn.Module):
    def __init__(self, vocab_size, emb_dim=300, rnn_hidden_dim=256, num_layers=1):
        super().__init__()
        self._rnn = nn.LSTM(input_size=emb_dim, hidden_size=rnn_hidden_dim, num_layers=num_layers, batch_first=True)
        self._out = nn.Linear(rnn_hidden_dim, vocab_size)

    def forward(self, inputs, hidden=None):
        out, hidden = self._rnn(inputs, hidden)
        return self._out(out), hidden


class MyModel(nn.Module):
    def __init__(self, target_vocab_size, emb_dim=300,
                 rnn_hidden_dim=256, embedding_matrix=None, decoder_layers=1, bidirectional_encoder=False, DEVICE='cpu',
                 vocab_stoi=None, vocab_itos=None):
        super().__init__()
        self.vocab_stoi = vocab_stoi
        self.vocab_itos = vocab_itos
        self.DEVICE = DEVICE
        self.emb = nn.Embedding(num_embeddings=target_vocab_size, embedding_dim=emb_dim)
        if embedding_matrix is not None:
            self.emb.weight = nn.Parameter(torch.tensor(embedding_matrix, dtype=torch.float32))
            self.emb.weight.requires_grad = False
        encoder = models.inception_v3(pretrained=True, aux_logits=False)
        encoder.eval()
        encoder.fc = nn.Linear(2048, rnn_hidden_dim, bias=True)
        for name, param in encoder.named_parameters():
            if name in ['fc.weight', 'fc.bias']:
                param.requires_grad = True
            else:
                param.requires_grad = False
        self.encoder = encoder
        self.decoder = Decoder(target_vocab_size, emb_dim, rnn_hidden_dim, num_layers=decoder_layers)
        self.dense = nn.Linear(rnn_hidden_dim, target_vocab_size)

    def forward(self, x):
        img, desc_token = x
        h_0 = self.encoder(img)
        c_0 = h_0
        desc_emb = self.emb(desc_token)
        out, _ = self.decoder.forward(desc_emb, (h_0.unsqueeze(0), c_0.unsqueeze(0)))
        return out

    def generate_caption(self, x):
        image = x
        h = self.encoder(image).unsqueeze(0)
        hidden = (h, h)
        output = []
        last_token = self.vocab_stoi['<s>']
        while (last_token != self.vocab_stoi['</s>'] and len(output) < 20):
            emb_input = self.emb(torch.LongTensor([last_token]).to(self.DEVICE))
            out, hidden = self.decoder.forward(emb_input.unsqueeze(0), hidden)
            last_token = int(torch.argmax(out))
            output.append(self.vocab_itos[last_token])
        return output


def load_model():
    DEVICE = 'cpu'  # 'cuda:0' if torch.cuda.is_available() else 'cpu'

    path = os.path.abspath(__file__)
    path = path[:path.find('model.py')]

    with open(f"{path}vocab_stoi.json", "r") as read_file:
        vocab_stoi = json.load(read_file)
    with open(f"{path}vocab_itos.json", "r") as read_file:
        vocab_itos = json.load(read_file)

    embedding_matrix = torch.load(f'{path}emb_matrix.pt')
    VOCAB_SIZE = embedding_matrix.shape[0]

    mm = MyModel(VOCAB_SIZE, embedding_matrix=embedding_matrix, DEVICE=DEVICE, vocab_itos=vocab_itos, vocab_stoi=vocab_stoi)
    mm.to(DEVICE)

    checkpoint = torch.load(f'{path}model_18_epochs.pth')
    mm.load_state_dict(checkpoint['model_state_dict'])
    return mm

if __name__ == '__main__':
    load_model()
