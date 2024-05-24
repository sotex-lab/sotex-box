import argparse
import os
import requests

def parse():
    parser = argparse.ArgumentParser(prog="Mass upload ads", description="Upload ads to an environment at once")
    parser.add_argument("url", help="Endpoint used to upload ads")
    parser.add_argument("files", help="Glob regex of files to upload", nargs='+', default=[])

    return parser.parse_args()

if __name__ == "__main__":
    args = parse()
    files = args.files
    if files is None or len(files) == 0:
        print("No files to upload")
        exit(1)

    if any([not os.path.exists(p) for p in files]):
        print("Some files do not exist")
        exit(1)

    print("Uploading ads to '%s' of files: '%s" % (args.url, args.files))

    for f in files:
        print("Uploading '%s' which will be named '%s'" % (f, os.path.basename(f)))
        response = requests.post(args.url, json={
            "tags": [
                "test"
            ]
        })

        if not (response.status_code >= 200 and response.status_code <= 299):
            print("Failed to create '%s', got response: %s" % (f, response.text))
            exit(1)

        print("Created '%s'" % f)
        s3_url = response.json()["presigned"]

        print("Uploading '%s' to '%s'" % (f, s3_url))
        response = requests.put(s3_url, data=open(f, 'rb'))

        if not (response.status_code >= 200 and response.status_code <= 299):
            print("Failed to upload '%s'" % f)
            exit(1)

        print("Uploaded '%s'" % f)

    print("Done")
