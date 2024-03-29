# Проект по курсу C# *"Веб-приложение для инференса нейросети"*

## Базовый сценарий
* Загрузить картинку в одном из форматов (jpg, png, jpeg).
* Дождаться окончания выполнения задачи применения нейросети.
* Скачать результат: подпись к картинке, сгенерированную нейросетью

## Набор эндпоинтов API

* POST /api/upload
  Загрузить картинку и поставить в очередь задачу инференса нейросети. Возвращает task_id (id задачи в очереди) и file_id (id файла загруженной картинки в хранилище).

* GET /api/{task_id}/status
  Узнать статус задачи: PENDING, STARTED, SUCCESS, FAILURE.

* GET /api/{task_id}/download
  Скачать результат при условии, что задача выполнена успешно. Результат представляет из себя фаил с текстом.

## Нейросеть
Encoder: inception_v3

Decoder: LSTM + attention + Linear

Эксперименты проводились тут: https://colab.research.google.com/drive/1wCvjuvM0eV2JgE7iBMVfnCdx-E9ePjaZ?usp=sharing
![](./images/image-captioning.png)

## Использование
* Запустить Worker и Server (например, в JetBrains Rider)
* Воспользовавшись SwaggerUI загрузить картинку
* Используя task_id, загрузить подпись к картинке

### Автор 
* Яремус Дмитрий
