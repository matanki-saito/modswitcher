#!/usr/bin/env python
# -*- coding:utf-8 -*-

import hashlib
import json
import os
import shutil
from boto3.session import Session
from os.path import join

_ = join


def upload_mod_to_s3(upload_dir_path,
                     name,
                     bucket_name,
                     access_key,
                     secret_access_key,
                     region):
    """
    S3にファイルをアップロードする
    :param upload_dir_path:
    :param name:
    :param bucket_name:
    :param access_key:
    :param secret_access_key:
    :param region:
    :return: CDNのURL
    """

    # ディレクトリをzip圧縮する
    out_zip_path = _(".", "tmp", "out.zip")
    tmp_zip_file_path = _(".", "tmp", "out")
    shutil.make_archive(tmp_zip_file_path, 'zip', root_dir=upload_dir_path)

    # セッション確立
    session = Session(aws_access_key_id=access_key,
                      aws_secret_access_key=secret_access_key,
                      region_name=region)

    s3 = session.resource('s3')
    s3.Bucket(bucket_name).upload_file(out_zip_path, name)

    return "{}/{}".format("https://d3fxmsw7mhzbqi.cloudfront.net", name), out_zip_path


def generate_distribution_file(url,
                               mod_file_path,
                               out_file_path):
    """
    trielaで使用する配布用設定ファイルを作成する。
    :param url:
    :param mod_file_path:
    :param out_file_path:
    :return:
    """

    with open(mod_file_path, 'rb') as fr:
        md5 = hashlib.md5(fr.read()).hexdigest()

    d_new = {'file_md5': md5,
             'url': url,
             'file_size': os.path.getsize(mod_file_path)}

    with open(out_file_path, "w", encoding="utf-8") as fw:
        json.dump(d_new, fw, indent=2, ensure_ascii=False)


def main():
    out_dir_path = _(".", "out")
    if os.path.exists(out_dir_path):
        shutil.rmtree(out_dir_path)
    os.makedirs(out_dir_path, exist_ok=True)

    # tmpファイルにexeを移す
    target_exe_path = _(".", "runner", "bin", "Release", "runner.exe")

    shutil.copy(target_exe_path, out_dir_path)

    # S3にアップロード from datetime import datetime as dt
    from datetime import datetime as dt
    cdn_url, mod_pack_file_path = upload_mod_to_s3(
        upload_dir_path=out_dir_path,
        name=dt.now().strftime('%Y-%m-%d_%H-%M-%S-{}.zip'.format("modswitcher")),
        bucket_name="triela-file",
        access_key=os.environ.get("AWS_S3_ACCESS_KEY"),
        secret_access_key=os.environ.get("AWS_S3_SECRET_ACCESS_KEY"),
        region="ap-northeast-1")

    print("cdn_url:{}".format(cdn_url))

    # distributionファイルを生成する
    generate_distribution_file(url=cdn_url,
                               out_file_path=_(".", "out", "dist.v2.json"),
                               mod_file_path=mod_pack_file_path)


if __name__ == "__main__":
    main()
