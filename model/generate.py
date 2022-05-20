import argparse
import torch
from torchvision import transforms
from PIL import Image
from pretrained_models.model import load_model

parser = argparse.ArgumentParser(description='A tutorial of argparse!')
parser.add_argument("--path_jpg")


def generate(mm, image_tensor):
    mm.eval()
    with torch.no_grad():
        words = mm.generate_caption(image_tensor.unsqueeze(0))
    return ' '.join(words[:-1])


def get_image(path):
    transform = transforms.Compose([
        transforms.Resize(299),
        transforms.CenterCrop(299),
        transforms.ToTensor(),
        transforms.Normalize([0.485, 0.456, 0.406], [0.229, 0.224, 0.225])
    ])
    image = Image.open(path)
    image.load()
    image = transform(image)
    return image


if __name__ == "__main__":
    args = parser.parse_args()
    PATH = args.path_jpg
    image = get_image(PATH)
    mm = load_model()
    mes = generate(mm, image)
    print(mes)

